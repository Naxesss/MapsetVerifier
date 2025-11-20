import {Flex} from "@mantine/core";
import PlaceholderBeatmapCard from "./PlaceholderBeatmapCard.tsx";

export default function PlaceholderBeatmapCards() {
  return (
    <Flex direction="column" gap="sm">
      <PlaceholderBeatmapCard/>
      <PlaceholderBeatmapCard/>
      <PlaceholderBeatmapCard/>
      <PlaceholderBeatmapCard/>
      <PlaceholderBeatmapCard/>
    </Flex>
  )
}