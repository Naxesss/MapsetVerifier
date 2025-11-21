import {Flex, Text} from "@mantine/core";
import {Beatmap} from "../../Types.ts";

function BeatmapCard ({ beatmap }: { beatmap: Beatmap }) {
    const bgStyle = beatmap.backgroundPath
        ? { backgroundImage: `url('http://localhost:5005${beatmap.backgroundPath}')` }
        : {};

    return (
        <Flex className="mapset-container" h={96} key={beatmap.folder}>
            <div className="mapset-bg" style={bgStyle} />
            <div className="mapset-bg-overlay" />
            <div className="mapset-text">
                <div className="mapset-title">
                  <Text fw="700">{beatmap.artist}</Text>
                  <Text fw="700">{beatmap.title}</Text>
                </div>
                <div className="mapset-creator">
                  <Text fs="italic">Mapped by {beatmap.creator}</Text>
                </div>
            </div>
        </Flex>
    );
}

export default BeatmapCard;
