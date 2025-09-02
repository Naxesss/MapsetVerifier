import { useSettings } from "../../context/SettingsContext";
import {useEffect} from "react";

export default function Beatmaps() {
    const {settings, setSettings} = useSettings();
    
    useEffect(() => {
        setSettings(prev => ({...prev, songFolder: 'C:/MySongs'}));
    }, [])
    
    return (
        <div className="beatmaps-container">
            <h2>Settings file</h2>
            <code>
                {JSON.stringify(settings, null, 2)}
            </code>
            
            <div className="beatmaps-item">
                
            </div>
        </div>
    )
}