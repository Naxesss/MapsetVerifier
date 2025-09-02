import Beatmaps from "../components/beatmaps/Beatmaps";

export default function Home() {
    return (
        <div className="page-container">
            <div className="screen-container">
                <div className="left panel">
                    <Beatmaps />
                </div>
                <div className="middle panel">
                    Middle
                </div>
                <div className="right panel">
                    Right
                </div>
            </div>
        </div>
    )
}