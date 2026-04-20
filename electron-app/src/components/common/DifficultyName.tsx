import {useSettings} from "../../context/SettingsContext.tsx";
import {DifficultyLevel, Mode} from "../../Types.ts";

interface DifficultyNameProps {
  difficulty: DifficultyLevel | null | undefined;
  mode: Mode | undefined;
}

function DifficultyName({ difficulty, mode }: DifficultyNameProps) {
  const { settings } = useSettings();
  
  if (!settings.showGamemodeDifficultyNames) {
    return difficulty;
  }
  
  switch (mode) {
    case "Standard":
    case "Mania":
      switch (difficulty) {
        case "Easy":
          return "Easy";
        case "Normal":
          return "Normal";
        case "Hard":
          return "Hard";
        case "Insane":
          return "Insane";
        case "Expert":
        case "Ultra":
          return "Expert";
        default:
          return difficulty;
      }
    case "Taiko":
      switch (difficulty) {
        case "Easy":
          return "Kantan";
        case "Normal":
          return "Futsuu";
        case "Hard":
          return "Muzukashii";
        case "Insane":
          return "Oni";
        case "Expert":
        case "Ultra":
          return "Inner Oni";
        default:
          return difficulty;
      }
    case "Catch":
      switch (difficulty) {
        case "Easy":
          return "Cup";
        case "Normal":
          return "Salad";
        case "Hard":
          return "Platter";
        case "Insane":
          return "Rain";
        case "Expert":
        case "Ultra":
          return "Overdose";
      default:
        return difficulty;
      }
    default:
      return difficulty;
  }
}

export default DifficultyName;
