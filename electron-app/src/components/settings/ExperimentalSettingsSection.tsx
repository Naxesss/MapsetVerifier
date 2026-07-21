import { Badge, Button, Group, Switch, Text, TextInput, Tooltip } from '@mantine/core';
import { IconAlertTriangle, IconAnalyze, IconFolder } from '@tabler/icons-react';
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

  const pickLazerDataDir = async () => {
    try {
      const result = await window.electronAPI?.dialog.openFolder();
      if (typeof result === 'string') {
        setSettings((prev) => ({ ...prev, lazerDataDir: result }));
      }
    } catch (e: any) {
      console.error('[Settings] Lazer data folder pick failed:', e);
      const msg = typeof e === 'string' ? e : e?.message || 'Unknown error';
      alert('Folder picker failed: ' + msg);
    }
  };

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
          description="Browse and check your osu!(lazer) beatmap library directly, at any time."
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
        {settings.lazerLookupEnabled && (
          <Group align="flex-end" gap="sm" wrap="nowrap">
            <TextInput
              label="osu!(lazer) data folder"
              description="Contains client.realm. Auto-detected when left empty."
              value={settings.lazerDataDir ?? ''}
              readOnly
              style={{ flex: 1, minWidth: 0 }}
              onClick={() => !settings.lazerDataDir && pickLazerDataDir()}
            />
            <Button
              size="sm"
              variant="light"
              leftSection={<IconFolder size={18} />}
              onClick={pickLazerDataDir}
            >
              Browse
            </Button>
          </Group>
        )}
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
