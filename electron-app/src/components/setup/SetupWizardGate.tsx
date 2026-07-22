import {
  Button,
  Container,
  Group,
  MantineProvider,
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
import { BeatmapViewMode, useSettings } from '../../context/SettingsContext.tsx';
import { cssVarResolver } from '../../theme/cssVarResolver';
import { useAppTheme } from '../../theme/useAppTheme';
import MinorIcon from '../icons/MinorIcon';
import { SettingsRow } from '../settings/SettingsSection.tsx';

interface SetupWizardGateProps {
  children: ReactNode;
}

const LAST_STEP = 2;

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
          <Stack gap={4}>
            <Title order={2}>Welcome to Mapset Verifier</Title>
            <Text c="dimmed" size="sm">
              Let&apos;s get a few things set up before you start.
            </Text>
          </Stack>

          <Stepper
            active={step}
            onStepClick={(next) => next <= step && setStep(next)}
            allowNextStepsSelect={false}
          >
            <Stepper.Step label="Library" description="Choose your beatmap source">
              <Stack gap="sm" mt="md">
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
                {viewMode !== 'stable' && (
                  <Text size="xs" c="dimmed">
                    The &quot;currently open in editor&quot; shortcut for osu!(lazer) is
                    Windows-only; browsing and checking maps works on any platform.
                  </Text>
                )}
              </Stack>
            </Stepper.Step>

            <Stepper.Step label="Folders" description="Confirm detected locations">
              <Stack gap="md" mt="md">
                {showSongFolder && (
                  <Group align="flex-end" gap="sm" wrap="nowrap">
                    <TextInput
                      label="osu! Songs Folder"
                      placeholder={settings.songFolder ? undefined : 'Detecting…'}
                      value={settings.songFolder ?? ''}
                      readOnly
                      style={{ flex: 1, minWidth: 0 }}
                      onClick={() => !settings.songFolder && pickFolder()}
                    />
                    <Button
                      size="sm"
                      variant="light"
                      leftSection={<IconFolder size={18} />}
                      onClick={pickFolder}
                    >
                      Browse
                    </Button>
                  </Group>
                )}
                {showLazerDataDir && (
                  <Group align="flex-end" gap="sm" wrap="nowrap">
                    <TextInput
                      label="osu!(lazer) data folder"
                      description="Contains client.realm. Auto-detected when left empty."
                      placeholder={settings.lazerDataDir ? undefined : 'Detecting…'}
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
                {!showSongFolder && !showLazerDataDir && (
                  <Text size="sm" c="dimmed">
                    Nothing to configure here for the selected library.
                  </Text>
                )}
              </Stack>
            </Stepper.Step>

            <Stepper.Step label="Preferences" description="A couple of quick defaults">
              <Stack gap="sm" mt="md">
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
            </Stepper.Step>
          </Stepper>

          <Group justify="space-between">
            <Button
              variant="subtle"
              disabled={step === 0}
              onClick={() => setStep((s) => Math.max(0, s - 1))}
            >
              Back
            </Button>
            {step < LAST_STEP ? (
              <Button onClick={() => setStep((s) => Math.min(LAST_STEP, s + 1))}>Next</Button>
            ) : (
              <Button onClick={finish}>Get started</Button>
            )}
          </Group>
        </Stack>
      </Container>
    </MantineProvider>
  );
}
