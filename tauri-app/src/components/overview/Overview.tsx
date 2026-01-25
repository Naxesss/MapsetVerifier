import {Box, Button, Group, LoadingOverlay, SegmentedControl, Title, useMantineTheme} from "@mantine/core";
import BeatmapHeader from "../common/BeatmapHeader.tsx";
import {IconRefresh} from "@tabler/icons-react";
import {useBeatmapBackground} from "../checks/hooks/useBeatmapBackground.ts";
import {useBeatmap} from "../../context/BeatmapContext.tsx";
import AudioOverview from "./audio/AudioOverview.tsx";
import MetadataOverview from "./metadata/MetadataOverview.tsx";
import BeatmapOverview from "./beatmap/BeatmapOverview.tsx";
import {useState} from "react";

type Tab = "Metadata" | "Beatmap" | "Audio";

const TABS: Tab[] = ["Metadata", "Beatmap", "Audio"];

function Overview() {
  const theme = useMantineTheme();
  const { selectedFolder } = useBeatmap();
  const { bgUrl, isLoading } = useBeatmapBackground(selectedFolder);
  const [activeTab, setActiveTab] = useState<Tab>("Metadata");

  return (
    <Box
      h="100%"
      style={{
        fontFamily: theme.headings.fontFamily,
        position: 'relative',
        width: '100%',
        borderRadius: theme.radius.lg,
        overflow: 'hidden',
        boxShadow: '0 4px 32px rgba(0,0,0,0.4)',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'flex-start',
      }}
    >
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: "sm", blur: 2 }} />
      <BeatmapHeader bgUrl={bgUrl}>
        <Group gap="sm" justify="space-between" style={{ width: '100%' }}>
          <Group gap="sm">
            <Group p="xs" gap="xs" bg={theme.colors.dark[8]} style={{ borderRadius: theme.radius.md }}>
              <Button
                variant="default"
                size="xs"
                leftSection={<IconRefresh size={16} />}
                onClick={() => window.location.reload()}
              >
                Refresh
              </Button>
            </Group>
            <Title order={3}>Overview</Title>
          </Group>
          <SegmentedControl
            value={activeTab}
            onChange={(value) => setActiveTab(value as Tab)}
            data={TABS}
            size="xs"
          />
        </Group>
      </BeatmapHeader>
      <Box style={{ flex: 1, overflow: 'auto' }}>
        {activeTab === "Metadata" && <MetadataOverview />}
        {activeTab === "Beatmap" && <BeatmapOverview />}
        {activeTab === "Audio" && <AudioOverview />}
      </Box>
    </Box>
  );
}

export default Overview;
