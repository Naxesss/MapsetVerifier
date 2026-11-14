import type {
  AudioFileInfo,
  BackgroundInfo,
  HitSoundUsage,
  ResourcesInfo,
  StoryboardInfo,
  VideoInfo,
} from '../../../Types';

export type ResourceFile =
  | { kind: 'audio'; key: string; name: string; size: string; audio: AudioFileInfo }
  | { kind: 'video'; key: string; name: string; size: string; video: VideoInfo }
  | { kind: 'background'; key: string; name: string; size: string; background: BackgroundInfo }
  | { kind: 'storyboard'; key: string; name: string; size: string; storyboard: StoryboardInfo }
  | { kind: 'hitSound'; key: string; name: string; size: string; hitSound: HitSoundUsage };

export type ResourceKind = ResourceFile['kind'];

export interface ResourceGroup {
  kind: ResourceKind;
  label: string;
  files: ResourceFile[];
}

/** What a file is, under its name in the details. */
export const KIND_LABELS: Record<ResourceKind, string> = {
  audio: 'Song audio',
  video: 'Background video',
  background: 'Background',
  storyboard: 'Storyboard',
  hitSound: 'Hit sound',
};

/** The colour of a file's dot in the list, the same for every file of its kind. */
export const KIND_COLORS: Record<ResourceKind, string> = {
  audio: 'blue.4',
  video: 'violet.4',
  background: 'green.4',
  storyboard: 'pink.4',
  hitSound: 'orange.4',
};

/** Every file the mapset uses, grouped by kind in the order the list shows them. */
export function groupResourceFiles(resources: ResourcesInfo): ResourceGroup[] {
  const groups: ResourceGroup[] = [];
  const add = (kind: ResourceKind, label: string, files: ResourceFile[]) => {
    if (files.length > 0) groups.push({ kind, label, files });
  };

  const { audioFile, storyboard } = resources;
  add(
    'audio',
    'Audio',
    audioFile
      ? [
          {
            kind: 'audio',
            key: `audio:${audioFile.fileName}`,
            name: audioFile.fileName,
            size: audioFile.fileSizeFormatted,
            audio: audioFile,
          },
        ]
      : []
  );
  add(
    'video',
    resources.videos.length > 1 ? 'Videos' : 'Video',
    resources.videos.map((video) => ({
      kind: 'video',
      key: `video:${video.fileName}`,
      name: video.fileName,
      size: video.fileSizeFormatted,
      video,
    }))
  );
  add(
    'background',
    resources.backgrounds.length > 1 ? 'Backgrounds' : 'Background',
    resources.backgrounds.map((background) => ({
      kind: 'background',
      key: `background:${background.fileName}`,
      name: background.fileName,
      size: background.fileSizeFormatted,
      background,
    }))
  );

  const hasStoryboard =
    storyboard.hasOsb || storyboard.difficultySpecificStoryboards.some((d) => d.hasStoryboard);
  add(
    'storyboard',
    'Storyboard',
    hasStoryboard
      ? [
          {
            kind: 'storyboard',
            key: 'storyboard',
            name: storyboard.osbFileName ?? 'Difficulty storyboards',
            size: '',
            storyboard,
          },
        ]
      : []
  );
  add(
    'hitSound',
    'Hit sounds',
    resources.hitSounds.map((hitSound) => ({
      kind: 'hitSound',
      key: `hitSound:${hitSound.fileName}`,
      name: hitSound.fileName,
      size: hitSound.fileSizeFormatted,
      hitSound,
    }))
  );

  return groups;
}
