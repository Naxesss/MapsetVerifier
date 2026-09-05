import { Badge, Button, Group, Select, Switch, Text, TextInput, Tooltip } from '@mantine/core';
import { IconAlertTriangle, IconAnalyze, IconFolder, IconSearch } from '@tabler/icons-react';
import { useState } from 'react';
import AdvancedAudioWarningModal from './AdvancedAudioWarningModal';
import { SettingsRow, SettingsSection } from './SettingsSection';
import { parseTimestampOpenTarget, useSettings } from '../../context/SettingsContext';
import type { TimestampOpenTarget } from '../../electron-env';

const TIMESTAMP_OPEN_OPTIONS: { label: string; value: TimestampOpenTarget }[] = [
  { label: 'Currently open client', value: 'current' },
  { label: 'osu!(stable)', value: 'stable' },
  { label: 'osu!(lazer)', value: 'lazer' },
  { label: 'Custom command', value: 'custom' },
];

function timestampPathKey(
  target: Exclude<TimestampOpenTarget, 'current'>
): 'timestampOpenStablePath' | 'timestampOpenLazerPath' | 'timestampOpenCustomCommand' {
  if (target === 'stable') return 'timestampOpenStablePath';
  if (target === 'lazer') return 'timestampOpenLazerPath';
  return 'timestampOpenCustomCommand';
}

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
  const [advancedAudioConfirmOpened, setAdvancedAudioConfirmOpened] = useState(false);
  const timestampTarget = settings.timestampOpenTarget;
  const timestampPath =
    timestampTarget === 'stable'
      ? (settings.timestampOpenStablePath ?? '')
      : timestampTarget === 'lazer'
        ? (settings.timestampOpenLazerPath ?? '')
        : (settings.timestampOpenCustomCommand ?? '');

  const pickTimestampPath = async () => {
    if (timestampTarget === 'current') return;
    try {
      const result = await window.electronAPI?.dialog.openFile();
      if (typeof result !== 'string') return;
      const key = timestampPathKey(timestampTarget);
      setSettings((prev) => ({ ...prev, [key]: result }));
    } catch (e: any) {
      console.error('[Settings] File pick failed:', e);
      const msg = typeof e === 'string' ? e : e?.message || 'Unknown error';
      alert('File picker failed: ' + msg);
    }
  };

  const detectTimestampPath = async () => {
    if (timestampTarget !== 'stable' && timestampTarget !== 'lazer') return;
    try {
      const result = await window.electronAPI?.shell.detectOsuExecutable({
        client: timestampTarget,
        songsFolder: settings.songFolder,
      });
      if (typeof result === 'string' && result) {
        const key = timestampPathKey(timestampTarget);
        setSettings((prev) => ({ ...prev, [key]: result }));
        return;
      }
      alert(
        timestampTarget === 'stable'
          ? 'Could not find osu!(stable). Browse to osu!.exe, or osu-wine on Linux.'
          : 'Could not find osu!(lazer). Browse to the Lazer executable or app.'
      );
    } catch (e: any) {
      console.error('[Settings] Client detect failed:', e);
      const msg = typeof e === 'string' ? e : e?.message || 'Unknown error';
      alert('Could not detect the client: ' + msg);
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
          title={<ExperimentalLabel>Bookmark beatmapsets</ExperimentalLabel>}
          description="Pin beatmapsets for quick lookup in the sidebar, without scrolling to find them."
          control={
            <Switch
              checked={settings.bookmarksEnabled}
              onChange={(e) => {
                const checked = e.currentTarget.checked;
                setSettings((prev) => ({ ...prev, bookmarksEnabled: checked }));
              }}
            />
          }
        />
        <SettingsRow
          title={<ExperimentalLabel>Open timestamps with</ExperimentalLabel>}
          description="Which client timestamp clicks launch. Currently open client uses the running osu!, and falls back to the system osu:// handler if both or neither are open."
          control={
            <Select
              data={TIMESTAMP_OPEN_OPTIONS}
              value={timestampTarget}
              allowDeselect={false}
              w={220}
              onChange={(value) =>
                setSettings((prev) => ({
                  ...prev,
                  timestampOpenTarget: parseTimestampOpenTarget(value),
                }))
              }
            />
          }
        />
        {timestampTarget !== 'current' && (
          <Group align="flex-end" gap="sm" wrap="nowrap">
            <TextInput
              label={timestampTarget === 'custom' ? 'Custom command' : 'Client path'}
              description={
                timestampTarget === 'custom'
                  ? 'Use {url} for the timestamp link, or it is appended.'
                  : 'Leave empty to auto-detect. Browse to pin a path.'
              }
              placeholder={
                timestampTarget === 'custom'
                  ? 'osu-wine --osuhandler {url}'
                  : timestampTarget === 'stable'
                    ? 'osu!.exe or osu-wine'
                    : 'osu! Lazer executable or app'
              }
              value={timestampPath}
              style={{ flex: 1, minWidth: 0 }}
              onChange={(event) => {
                const key = timestampPathKey(timestampTarget);
                setSettings((prev) => ({ ...prev, [key]: event.currentTarget.value }));
              }}
            />
            <Button
              size="sm"
              variant="light"
              leftSection={<IconFolder size={18} />}
              onClick={pickTimestampPath}
            >
              Browse
            </Button>
            {timestampTarget !== 'custom' && (
              <Button
                size="sm"
                variant="light"
                leftSection={<IconSearch size={18} />}
                onClick={() => void detectTimestampPath()}
              >
                Detect
              </Button>
            )}
          </Group>
        )}
      </SettingsSection>
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
