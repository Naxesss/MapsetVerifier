import { useSettings } from "../../context/SettingsContext";
import BeatmapsList from "./BeatmapsList.tsx";
import "./Beatmaps.scss";

export default function Beatmaps() {
    const { settings } = useSettings();

    return (
        // Provide fallback empty string to satisfy required prop
        <BeatmapsList songFolder={settings.songFolder || ""} />
    );
}