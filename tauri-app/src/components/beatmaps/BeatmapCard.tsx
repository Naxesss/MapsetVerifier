import {Flex, Text} from "@mantine/core";
import {Beatmap} from "../../Types.ts";
import {Link} from "react-router-dom";

function BeatmapCard ({ beatmap }: { beatmap: Beatmap }) {
    const bgStyle = beatmap.folder
        ? { backgroundImage: `url('http://localhost:5005/beatmap/image?folder=${beatmap.folder}')` }
        : {};

    return (
        <Link to={`/checks/${beatmap.folder}`} style={{ textDecoration: 'none', color: 'inherit', width: '100%' }}>
            <Flex className="mapset-container" h={96} key={beatmap.folder} style={{ cursor: 'pointer' }}>
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
        </Link>
    );
}

export default BeatmapCard;
