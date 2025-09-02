import { useSettings } from "../../context/SettingsContext";
import BeatmapsList from "./BeatmapsList.tsx";
import "./Beatmaps.scss";

export default function Beatmaps() {
    const { settings } = useSettings();

    return (
        <BeatmapsList songFolder={settings.songFolder} />
    );
}