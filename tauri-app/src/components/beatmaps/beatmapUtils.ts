export function scrape(src: string, start: string, end: string): string {
    const s = src.indexOf(start);
    if (s === -1) return "";
    const e = src.indexOf(end, s + start.length);
    if (e === -1) return "";
    return src.substring(s + start.length, e).trim();
}

