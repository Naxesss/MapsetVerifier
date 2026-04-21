/** Irregular plurals are not supported; extend with a map or pass a different base word if needed. */
function pluralizeEnglish(singular: string): string {
    const lower = singular.toLowerCase();
    if (/[sxz]$/.test(lower) || /[cs]h$/.test(lower)) {
        return `${singular}es`;
    }
    if (/[^aeiou]y$/i.test(singular)) {
        return `${singular.slice(0, -1)}ies`;
    }
    return `${singular}s`;
}

/** Singular or plural form of `singular` for `count` (no leading number). */
export function pluralize(count: number, singular: string): string {
    return count === 1 ? singular : pluralizeEnglish(singular);
}

/** e.g. countWord(3, "warning") → "3 warnings" */
export function countWord(count: number, singular: string): string {
    return `${count} ${pluralize(count, singular)}`;
}
