import {
  Box,
  Button,
  Container,
  Divider,
  Group,
  MantineProvider,
  Paper,
  SegmentedControl,
  Stack,
  Stepper,
  Switch,
  Text,
  TextInput,
  Title,
} from '@mantine/core';
import { IconFolder } from '@tabler/icons-react';
import { ReactNode, useState } from 'react';
import logoUrl from '../../assets/logo.svg';
import { BeatmapViewMode, useSettings } from '../../context/SettingsContext.tsx';
import { cssVarResolver } from '../../theme/cssVarResolver';
import { useAppTheme } from '../../theme/useAppTheme';
import MinorIcon from '../icons/MinorIcon';
import { SettingsRow } from '../settings/SettingsSection.tsx';

interface SetupWizardGateProps {
  children: ReactNode;
}

const LAST_STEP = 2;
const STEP_CONTENT_MIN_HEIGHT = 188;

const STEPS = [
  { label: 'Library', description: 'Choose your beatmap source' },
  { label: 'Folders', description: 'Confirm detected locations' },
  { label: 'Preferences', description: 'A couple of quick defaults' },
] as const;

/**
 * First-launch setup wizard. Shown to every user (not just fresh installs) until they complete
 * it once, since beatmapViewMode/folder settings didn't exist for existing installs either.
 * Rendered after BackendGate so it can rely on the backend's folder auto-detect endpoints.
 */
export default function SetupWizardGate({ children }: SetupWizardGateProps) {
  const { settings, setSettings, loaded } = useSettings();
  const theme = useAppTheme();
  const [step, setStep] = useState(0);

  if (!loaded || settings.hasCompletedSetup) {
    return <>{children}</>;
  }

  const viewMode = settings.beatmapViewMode;
  const showSongFolder = viewMode === 'stable' || viewMode === 'both';
  const showLazerDataDir = viewMode === 'lazer' || viewMode === 'both';

  const pickFolder = async () => {
    try {
      const result = await window.electronAPI?.dialog.openFolder();
      if (typeof result === 'string') {
        setSettings((prev) => ({ ...prev, songFolder: result }));
      }
    } catch (e: any) {
      console.error('[Setup] Folder pick failed:', e);
    }
  };

  const pickLazerDataDir = async () => {
    try {
      const result = await window.electronAPI?.dialog.openFolder();
      if (typeof result === 'string') {
        setSettings((prev) => ({ ...prev, lazerDataDir: result }));
      }
    } catch (e: any) {
      console.error('[Setup] Lazer data folder pick failed:', e);
    }
  };

  const finish = () => setSettings((prev) => ({ ...prev, hasCompletedSetup: true }));

  return (
    <MantineProvider defaultColorScheme="dark" theme={theme} cssVariablesResolver={cssVarResolver}>
      <Container size="md" pt={80}>
        <Stack gap="lg">
          <Group gap="md" wrap="nowrap" align="center">
            <Box aria-hidden style={{ flexShrink: 0, opacity: 0.92 }}>
              <Paper radius="lg" p="sm" bg="dark.8">
                <img
                  src={logoUrl}
                  alt=""
                  width={50}
                  height={50}
                  draggable={false}
                  style={{ display: 'block' }}
                />
              </Paper>
            </Box>
            <Stack gap={4} style={{ minWidth: 0 }}>
              <Title order={2}>Welcome to Mapset Verifier</Title>
              <Text c="dimmed" size="sm">
                Let&apos;s get a few things set up before you start.
              </Text>
            </Stack>
          </Group>

          <Stepper
            active={step}
            onStepClick={(next) => next <= step && setStep(next)}
            allowNextStepsSelect={false}
            wrap={false}
            styles={{ content: { display: 'none', padding: 0 } }}
          >
            {STEPS.map((item) => (
              <Stepper.Step key={item.label} label={item.label} description={item.description} />
            ))}
          </Stepper>

          <Box mih={STEP_CONTENT_MIN_HEIGHT}>
            {step === 0 && (
              <Stack gap="sm">
                <Text size="sm">Which beatmap library should Mapset Verifier read from?</Text>
                <SegmentedControl
                  fullWidth
                  data={[
                    { label: 'Stable', value: 'stable' },
                    { label: 'Lazer', value: 'lazer' },
                    { label: 'Both', value: 'both' },
                  ]}
                  value={viewMode}
                  onChange={(value) =>
                    setSettings((prev) => ({
                      ...prev,
                      beatmapViewMode: value as BeatmapViewMode,
                    }))
                  }
                />
                <Text
                  size="xs"
                  c="dimmed"
                  style={{
                    minHeight: '2.75em',
                    visibility: viewMode === 'stable' ? 'hidden' : 'visible',
                  }}
                >
                  The &quot;currently open in editor&quot; shortcut for osu!(lazer) is Windows-only;
                  browsing and checking maps works on any platform.
                </Text>
              </Stack>
            )}

            {step === 1 && (
              <Stack gap="md">
                {showSongFolder && (
                  <FolderField
                    label="osu! Songs Folder"
                    placeholder={settings.songFolder ? undefined : 'Detecting…'}
                    value={settings.songFolder ?? ''}
                    onBrowse={pickFolder}
                  />
                )}
                {showLazerDataDir && (
                  <FolderField
                    label="osu!(lazer) data folder"
                    description="Contains client.realm. Auto-detected when left empty."
                    placeholder={settings.lazerDataDir ? undefined : 'Detecting…'}
                    value={settings.lazerDataDir ?? ''}
                    onBrowse={pickLazerDataDir}
                  />
                )}
                {!showSongFolder && !showLazerDataDir && (
                  <Text size="sm" c="dimmed">
                    Nothing to configure here for the selected library.
                  </Text>
                )}
              </Stack>
            )}

            {step === 2 && (
              <Stack gap="sm">
                <SettingsRow
                  title={
                    <Group gap="xs" align="center" wrap="nowrap">
                      <MinorIcon size={16} />
                      Show negligible issues
                    </Group>
                  }
                  description="Includes negligible findings in the checks page."
                  control={
                    <Switch
                      checked={settings.showMinor}
                      onChange={(e) => {
                        const checked = e.currentTarget.checked;
                        setSettings((prev) => ({ ...prev, showMinor: checked }));
                      }}
                    />
                  }
                />
                <SettingsRow
                  title="Receive beta updates"
                  description="Includes prerelease versions when checking for updates."
                  control={
                    <Switch
                      checked={settings.receivePrereleases}
                      onChange={(e) => {
                        const checked = e.currentTarget.checked;
                        setSettings((prev) => ({ ...prev, receivePrereleases: checked }));
                      }}
                    />
                  }
                />
              </Stack>
            )}
          </Box>

          <Divider />

          <Group justify="space-between">
            <Button
              variant="subtle"
              disabled={step === 0}
              onClick={() => setStep((s) => Math.max(0, s - 1))}
            >
              Back
            </Button>
            <Button
              miw={132}
              onClick={() =>
                step < LAST_STEP ? setStep((s) => Math.min(LAST_STEP, s + 1)) : finish()
              }
            >
              {step < LAST_STEP ? 'Next' : 'Get started'}
            </Button>
          </Group>
        </Stack>
      </Container>
    </MantineProvider>
  );
}

function FolderField({
  label,
  description,
  placeholder,
  value,
  onBrowse,
}: {
  label: string;
  description?: string;
  placeholder?: string;
  value: string;
  onBrowse: () => void;
}) {
  return (
    <Group align="flex-end" gap="sm" wrap="nowrap">
      <TextInput
        label={label}
        description={description}
        placeholder={placeholder}
        value={value}
        readOnly
        style={{ flex: 1, minWidth: 0 }}
        onClick={() => !value && onBrowse()}
      />
      <Button size="sm" variant="light" leftSection={<IconFolder size={18} />} onClick={onBrowse}>
        Browse
      </Button>
    </Group>
  );
}
