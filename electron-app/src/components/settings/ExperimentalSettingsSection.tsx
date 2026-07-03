import { Badge, Group, Switch, Text, Tooltip } from '@mantine/core';
import { IconAlertTriangle, IconAnalyze } from '@tabler/icons-react';
import { useState } from 'react';
import AdvancedAudioWarningModal from './AdvancedAudioWarningModal';
import LazerLookupWarningModal from './LazerLookupWarningModal';
import { SettingsRow, SettingsSection } from './SettingsSection';
import { useSettings } from '../../context/SettingsContext';

function ExperimentalLabel({ children }: { children: React.ReactNode }) {
  return (
    <Group gap="xs" align="center" wrap="nowrap">
      <Text size="sm">{children}</Text>
      <Tooltip label="Experimental">
        <Badge
          size="xs"
          radius="xl"
          variant="light"
          color="yellow"
          px={6}
          aria-label="Experimental setting"
          leftSection={<IconAlertTriangle size={11} />}
        />
      </Tooltip>
    </Group>
  );
}

export default function ExperimentalSettingsSection() {
  const { settings, setSettings } = useSettings();
  const [lazerWarningOpened, setLazerWarningOpened] = useState(false);
  const [advancedAudioConfirmOpened, setAdvancedAudioConfirmOpened] = useState(false);

  return (
    <>
      <SettingsSection
        icon={<IconAnalyze size={28} />}
        title="Experimental"
        description="Optional features that are still being tested or may not be useful for every workflow."
      >
        <SettingsRow
          title={<ExperimentalLabel>Show advanced audio analysis</ExperimentalLabel>}
          description="Displays additional technical audio information in the overview."
          control={
            <Switch
              checked={settings.showAdvancedAudioAnalysis}
              onChange={(e) => {
                const checked = e.currentTarget.checked;
                if (!checked) {
                  setSettings((prev) => ({ ...prev, showAdvancedAudioAnalysis: false }));
                  return;
                }

                if (!settings.showAdvancedAudioAnalysis) {
                  setAdvancedAudioConfirmOpened(true);
                }
              }}
            />
          }
        />
        <SettingsRow
          title={<ExperimentalLabel>osu!(lazer) support</ExperimentalLabel>}
          description="Detects beatmaps opened from the lazer editor when supported."
          control={
            <Switch
              checked={settings.lazerLookupEnabled}
              onChange={(e) => {
                const checked = e.currentTarget.checked;
                if (!checked) {
                  setSettings((prev) => ({ ...prev, lazerLookupEnabled: false }));
                  return;
                }

                if (!settings.lazerLookupEnabled) {
                  setLazerWarningOpened(true);
                }
              }}
            />
          }
        />
        <SettingsRow
          title={<ExperimentalLabel>Bookmark beatmapsets</ExperimentalLabel>}
          description="Pin beatmapsets for quick lookup in the sidebar, without scrolling to find them."
          control={
            <Switch
              checked={settings.bookmarksEnabled}
              onChange={(e) => {
                setSettings((prev) => ({ ...prev, bookmarksEnabled: e.currentTarget.checked }));
              }}
            />
          }
        />
      </SettingsSection>
      <LazerLookupWarningModal
        opened={lazerWarningOpened}
        onCancel={() => setLazerWarningOpened(false)}
        onConfirm={() => {
          setSettings((prev) => ({ ...prev, lazerLookupEnabled: true }));
          setLazerWarningOpened(false);
        }}
      />
      <AdvancedAudioWarningModal
        opened={advancedAudioConfirmOpened}
        onCancel={() => setAdvancedAudioConfirmOpened(false)}
        onConfirm={() => {
          setSettings((prev) => ({ ...prev, showAdvancedAudioAnalysis: true }));
          setAdvancedAudioConfirmOpened(false);
        }}
      />
    </>
  );
}
