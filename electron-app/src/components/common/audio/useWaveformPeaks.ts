import { useQuery } from '@tanstack/react-query';

/** How finely the file is summed up; bars are drawn from these, a few per bucket at most. */
const BUCKETS = 4096;
/** Above this size (the song) the file is decoded at a low rate: plenty for its shape, far less memory. */
const LARGE_FILE_BYTES = 2 * 1024 * 1024;
const LARGE_FILE_SAMPLE_RATE = 8000;
const SMALL_FILE_SAMPLE_RATE = 44100;

export interface WaveformPeaks {
  /** The loudest sample of each slice of the file, 0 to 1, over all channels. */
  peaks: Float32Array;
  durationMs: number;
  /** The file holds no audio at all, e.g. the 44-byte WAV used to silence a hit sound. */
  isEmpty: boolean;
}

const EMPTY: WaveformPeaks = { peaks: new Float32Array(0), durationMs: 0, isEmpty: true };

/** Whether a WAV file's sample data is empty: just a header, which browsers refuse to decode. */
function isEmptyWav(bytes: ArrayBuffer) {
  const view = new DataView(bytes);
  const tag = (offset: number) => String.fromCharCode(...new Uint8Array(bytes, offset, 4));
  if (bytes.byteLength < 12 || tag(0) !== 'RIFF' || tag(8) !== 'WAVE') return false;

  let offset = 12;
  while (offset + 8 <= bytes.byteLength) {
    const size = view.getUint32(offset + 4, true);
    if (tag(offset) === 'data') return size === 0 || offset + 8 >= bytes.byteLength;
    offset += 8 + size + (size % 2);
  }
  // No data chunk at all.
  return true;
}

async function loadPeaks(url: string): Promise<WaveformPeaks> {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`Could not load the audio (${response.status}).`);
  const bytes = await response.arrayBuffer();
  if (bytes.byteLength === 0 || isEmptyWav(bytes)) return EMPTY;

  const sampleRate =
    bytes.byteLength > LARGE_FILE_BYTES ? LARGE_FILE_SAMPLE_RATE : SMALL_FILE_SAMPLE_RATE;
  const context = new OfflineAudioContext(1, 1, sampleRate);
  const buffer = await context.decodeAudioData(bytes);
  if (buffer.length === 0) return EMPTY;

  const length = buffer.length;
  const bucketCount = Math.max(1, Math.min(BUCKETS, length));
  const peaks = new Float32Array(bucketCount);
  for (let channel = 0; channel < buffer.numberOfChannels; channel++) {
    const data = buffer.getChannelData(channel);
    for (let bucket = 0; bucket < bucketCount; bucket++) {
      const start = Math.floor((bucket * length) / bucketCount);
      const end = Math.floor(((bucket + 1) * length) / bucketCount);
      let peak = peaks[bucket];
      for (let i = start; i < end; i++) {
        const value = Math.abs(data[i]);
        if (value > peak) peak = value;
      }
      peaks[bucket] = Math.min(1, peak);
    }
  }

  return { peaks, durationMs: buffer.duration * 1000, isEmpty: false };
}

/** The shape of an audio file, worked out here from the file itself and kept for the session. */
export function useWaveformPeaks(url: string | null) {
  return useQuery({
    queryKey: ['waveform-peaks', url],
    queryFn: () => loadPeaks(url!),
    enabled: !!url,
    staleTime: Infinity,
    gcTime: 10 * 60 * 1000,
    retry: false,
  });
}
