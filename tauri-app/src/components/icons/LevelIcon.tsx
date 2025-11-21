import NoIssueIcon from "./NoIssueIcon.tsx";
import ErrorIcon from "./ErrorIcon.tsx";
import ProblemIcon from "./ProblemIcon.tsx";
import WarningIcon from "./WarningIcon.tsx";
import MinorIcon from "./MinorIcon.tsx";
import {Text} from "@mantine/core";
import {Level} from "../../Types.ts";

function LevelIcon({ level }: { level: Level }) {
  if (level === "Info") {
    return <NoIssueIcon/>;
  } else if (level === "Error") {
    return <ErrorIcon/>;
  } else if (level === "Problem") {
    return <ProblemIcon/>;
  } else if (level === "Warning") {
    return <WarningIcon/>;
  } else if (level === "Minor") {
    return <MinorIcon/>;
  } else {
    return <Text>Missing Icon</Text>;
  }
}

export default LevelIcon;