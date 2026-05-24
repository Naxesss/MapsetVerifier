export type SseMessage = {
  event: string;
  data: string;
};

export function parseSseChunk(buffer: string): { messages: SseMessage[]; remaining: string } {
  const messages: SseMessage[] = [];
  const parts = buffer.split('\n\n');
  const remaining = parts.pop() ?? '';

  for (const part of parts) {
    if (!part.trim()) continue;

    let event = 'message';
    let data = '';

    for (const line of part.split('\n')) {
      if (line.startsWith('event:')) {
        event = line.slice(6).trim();
      } else if (line.startsWith('data:')) {
        data += line.slice(5).trim();
      }
    }

    if (data) {
      messages.push({ event, data });
    }
  }

  return { messages, remaining };
}
