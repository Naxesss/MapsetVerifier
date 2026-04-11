import {Box, Button, Group, LoadingOverlay, SegmentedControl, Tooltip, useMantineTheme} from "@mantine/core";
import {IconRefresh} from "@tabler/icons-react";
import {useState} from "react";
import AudioOverview from "./audio/AudioOverview.tsx";
import BeatmapOverview from "./beatmap/BeatmapOverview.tsx";
import MetadataOverview from "./metadata/MetadataOverview.tsx";
import ObjectsOverview from "./objects/ObjectsOverview.tsx";
import {useBeatmap} from "../../context/BeatmapContext.tsx";
import {useBeatmapBackground} from "../checks/hooks/useBeatmapBackground.ts";
import BeatmapHeader from "../common/BeatmapHeader.tsx";

type Tab = "Metadata" | "Beatmap" | "Audio" | "Objects";

const TABS: Tab[] = ["Metadata", "Objects", "Beatmap", "Audio"];

function Overview() {
  const theme = useMantineTheme();
  const { selectedFolder } = useBeatmap();
  const { bgUrl, isLoading } = useBeatmapBackground(selectedFolder);
  const [activeTab, setActiveTab] = useState<Tab>("Metadata");
  const [reloadFlag, setReloadFlag] = useState(0);

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
              <Tooltip label="Reparse the beatmap">
                <Button
                  size="xs"
                  variant="default"
                  onClick={() => setReloadFlag(f => f + 1)}
                >
                  <IconRefresh />
                </Button>
              </Tooltip>
            </Group>
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
        {activeTab === "Metadata" && <MetadataOverview reloadFlag={reloadFlag} />}
        {activeTab === "Beatmap" && <BeatmapOverview reloadFlag={reloadFlag} />}
        {activeTab === "Audio" && <AudioOverview reloadFlag={reloadFlag} />}
        {activeTab === "Objects" && <ObjectsOverview reloadFlag={reloadFlag} />}
      </Box>
    </Box>
  );
}

export default Overview;
