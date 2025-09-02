import { useEffect, useState } from "react";
import { readDir, readTextFile, stat } from "@tauri-apps/plugin-fs";
import { join } from "@tauri-apps/api/path";
import { Beatmap } from "./BeatmapTypes";
import { scrape } from "./beatmapUtils";
import BeatmapCard from "./BeatmapCard";
import "./Beatmaps.scss";

interface Props {
    songFolder: string;
}

export default function BeatmapsList({ songFolder }: Props) {
    const [beatmaps, setBeatmaps] = useState<Beatmap[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [search, setSearch] = useState("");
    const stepSize = 16;

    function isValidSongFolder(songFolder: string | undefined): boolean {
        return !!songFolder;
    }

    async function getBeatmapFolders(songFolder: string) {
        const entries = await readDir(songFolder);
        // Filter for directories and fetch their mtime
        const foldersWithMtime = await Promise.all(
            entries
                .filter(e => e.name && e.isDirectory)
                .map(async e => {
                    try {
                        const statResult = await stat(await join(songFolder, e.name));
                        return { name: e.name, isDirectory: e.isDirectory, mtime: statResult.mtime?.getTime() };
                    } catch {
                        return { name: e.name, isDirectory: e.isDirectory, mtime: 0 };
                    }
                })
        );
        // Sort by mtime descending (newest first)
        foldersWithMtime.sort((a, b) =>{
            return (b.mtime || 0) - (a.mtime || 0)
        });
        return foldersWithMtime;
    }

    async function getOsuFilesInFolder(folderPath: string) {
        try {
            const files = await readDir(folderPath);
            return files.filter(f => f.name && f.name.endsWith(".osu"));
        } catch {
            console.error(`[Beatmaps] Folder can't be read: ${folderPath}`);
            return [];
        }
    }

    async function parseBeatmapMetadata(folderPath: string, data: string) {
        const title = scrape(data, "Title:", "\n");
        const artist = scrape(data, "Artist:", "\n");
        const creator = scrape(data, "Creator:", "\n");
        const beatmapID = scrape(data, "BeatmapID:", "\n");
        const beatmapSetID = scrape(data, "BeatmapSetID:", "\n");
        const bgFile = scrape(data, "0,0,\"", "\"");
        const backgroundPath = bgFile ? await join(folderPath, bgFile) : "";
        return { title, artist, creator, beatmapID, beatmapSetID, backgroundPath };
    }

    function matchesSearch(bm: any, search: string) {
        const searchableString = `${bm.title} - ${bm.artist} | ${bm.creator} (${bm.beatmapID} ${bm.beatmapSetID})`;
        return !search || searchableString.toLowerCase().includes(search.toLowerCase());
    }

    async function loadBeatmaps() {
        setLoading(true);
        setError(null);
        setBeatmaps([]);
        if (!isValidSongFolder(songFolder)) {
            setError("No song folder selected. Please select your osu! Songs folder.");
            setLoading(false);
            return;
        }
        try {
            console.log("[Beatmaps] Reading song folder:", songFolder);
            const folders = await getBeatmapFolders(songFolder);
            let found = 0;
            const results: Beatmap[] = [];
            for (const folderEntry of folders) {
                if (found >= stepSize) break;
                const folderPath = await join(songFolder, folderEntry.name);
                console.log(`[Beatmaps] Reading beatmap folder: ${folderPath}`);
                const osuFiles = await getOsuFilesInFolder(folderPath);
                for (const fileEntry of osuFiles) {
                    const filePath = await join(folderPath, fileEntry.name);
                    console.log(`[Beatmaps] Reading .osu file: ${filePath}`);
                    const data = await readTextFile(filePath);
                    const meta = await parseBeatmapMetadata(folderPath, data);
                    console.log(meta);
                    if (matchesSearch(meta, search)) {
                        results.push({
                            folder: folderEntry.name,
                            ...meta,
                        });
                        found++;
                    }
                    // Only need one .osu file per folder to get song info
                    break;
                }
            }
            setBeatmaps(results);
            if (results.length === 0) {
                console.log("[Beatmaps] No beatmaps found.");
                setError(search ? "The search yielded no results." : "No mapsets could be found in this folder. Make sure you've selected /osu!/Songs.");
            } else {
                console.log(`[Beatmaps] Loaded ${results.length} beatmaps.`);
            }
        } catch (e) {
            console.error("[Beatmaps] Failed to load beatmaps:", (e as Error).message);
            setError("Failed to load beatmaps: " + (e as Error).message);
        }
        setLoading(false);
    }

    useEffect(() => {
        loadBeatmaps();
    }, [songFolder, search]);

    return (
        <div className="beatmaps-container">
            <div className="beatmaps">
                <input
                    type="text"
                    placeholder="Search beatmaps..."
                    value={search}
                    onChange={e => setSearch(e.target.value)}
                />
                {loading && <div className="large-search-icon loading-icon" style={{ textAlign: "center" }}></div>}
                {error && <div id="left-content-mapsets-empty">{error}</div>}
                <div className="beatmaps-list">
                    {beatmaps.map(bm => (
                        <BeatmapCard key={bm.folder} beatmap={bm} />
                    ))}
                </div>
            </div>
        </div>
    );
}