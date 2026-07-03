import {
  Modal,
  Button,
  TextInput,
  Switch,
  Select,
  Group,
  Stack,
  Alert,
  Divider,
  Text,
  Badge,
  Tooltip,
  Box,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import {
  IconAlertTriangle,
  IconBrandGithub,
  IconFolder,
  IconNote,
  IconRefresh,
  IconWorld,
} from '@tabler/icons-react';
import React, { useEffect, useState } from 'react';
import AdvancedAudioWarningModal from './AdvancedAudioWarningModal';
import LazerLookupWarningModal from './LazerLookupWarningModal';
import MinorChecksFilterModal from './MinorChecksFilterModal';
import PluginManager from './PluginManager';
import { SOURCE_CODE_URL, WEBSITE_URL } from '../../Constants.ts';
import { useDocumentation } from '../../context/DocumentationContext.tsx';
import { useSettings } from '../../context/SettingsContext';
import { useUpdater } from '../../context/UpdaterContext';
import { useOpenExternal } from '../../hooks/useOpenExternal';
import {
  DEFAULT_UI_FONT_FAMILY,
  UI_FONT_FAMILY_OPTIONS,
  parseUiFontFamily,
  type UiFontFamily,
} from '../../theme/fonts';
import MinorIcon from '../icons/MinorIcon';

interface SettingsModalProps {
  opened: boolean;
  onClose: () => void;
}

const SettingsModal: React.FC<SettingsModalProps> = ({ opened, onClose }) => {
  const { settings, setSettings } = useSettings();
  const { checkForUpdates, openUpdater, currentVersion, currentVersionIsPrerelease } = useUpdater();
  const openExternal = useOpenExternal();
  const [songFolder, setSongFolder] = useState(settings.songFolder ?? '');
  const [showMinor, setShowMinor] = useState(settings.showMinor);
  const [showGamemodeDifficultyNames, setShowGamemodeDifficultyNames] = useState(
    settings.showGamemodeDifficultyNames
  );
  const [showAdvancedAudioAnalysis, setShowAdvancedAudioAnalysis] = useState(
    settings.showAdvancedAudioAnalysis
  );
  const [lazerLookupEnabled, setLazerLookupEnabled] = useState(settings.lazerLookupEnabled);
  const [receivePrereleases, setReceivePrereleases] = useState(settings.receivePrereleases);
  const [gateInDev, setGateInDev] = useState(settings.gateInDev);
  const [goToChecksOnMapsetSwitch, setGoToChecksOnMapsetSwitch] = useState(
    settings.goToChecksOnMapsetSwitch
  );
  const [showCheckRunDelta, setShowCheckRunDelta] = useState(settings.showCheckRunDelta);
  const [checkRunDeltaShowUnchanged, setCheckRunDeltaShowUnchanged] = useState(
    settings.checkRunDeltaShowUnchanged
  );
  const [uiFontFamily, setUiFontFamily] = useState<UiFontFamily>(
    parseUiFontFamily(settings.uiFontFamily)
  );
  const [lazerWarningOpened, setLazerWarningOpened] = useState(false);
  const [advancedAudioConfirmOpened, setAdvancedAudioConfirmOpened] = useState(false);
  const [minorFilterOpened, { open: openMinorFilterModal, close: closeMinorFilterModal }] =
    useDisclosure(false);

  const { status: docsStatus } = useDocumentation();

  // Keep local state in sync when modal is opened or settings change asynchronously
  useEffect(() => {
    if (opened) {
      setSongFolder(settings.songFolder ?? '');
      setShowMinor(settings.showMinor);
      setShowGamemodeDifficultyNames(settings.showGamemodeDifficultyNames);
      setShowAdvancedAudioAnalysis(settings.showAdvancedAudioAnalysis);
      setLazerLookupEnabled(settings.lazerLookupEnabled);
      setReceivePrereleases(settings.receivePrereleases);
      setGateInDev(settings.gateInDev);
      setGoToChecksOnMapsetSwitch(settings.goToChecksOnMapsetSwitch);
      setShowCheckRunDelta(settings.showCheckRunDelta);
      setCheckRunDeltaShowUnchanged(settings.checkRunDeltaShowUnchanged);
      setUiFontFamily(parseUiFontFamily(settings.uiFontFamily));
    }
  }, [
    opened,
    settings.songFolder,
    settings.showMinor,
    settings.showGamemodeDifficultyNames,
    settings.showAdvancedAudioAnalysis,
    settings.lazerLookupEnabled,
    settings.receivePrereleases,
    settings.gateInDev,
    settings.goToChecksOnMapsetSwitch,
    settings.showCheckRunDelta,
    settings.checkRunDeltaShowUnchanged,
    settings.uiFontFamily,
  ]);

  const pickFolder = async () => {
    try {
      const result = await window.electronAPI?.dialog.openFolder();
      if (typeof result === 'string') {
        setSongFolder(result);
        setSettings((prev) => ({ ...prev, songFolder: result }));
      }
    } catch (e: any) {
      console.error('[SettingsModal] Folder pick failed:', e);
      const msg = typeof e === 'string' ? e : e?.message || 'Unknown error';
      alert('Folder picker failed: ' + msg);
    }
  };

  const openFolder = async (getPath: () => Promise<string | undefined>) => {
    try {
      const folderPath = await getPath();
      if (!folderPath) return;
      const err = await window.electronAPI?.shell.openPath(folderPath);
      if (err) throw new Error(err);
    } catch (e) {
      console.error('[SettingsModal] Failed to open folder:', e);
      alert('Failed to open folder. See console for details.');
    }
  };

  const openAppFolder = () =>
    openFolder(() => window.electronAPI?.app.getAppFolderPath() ?? Promise.resolve(undefined));

  const openExternalsFolder = () =>
    openFolder(
      () => window.electronAPI?.app.getExternalsFolderPath() ?? Promise.resolve(undefined)
    );

  const isDev = import.meta.env.DEV;

  const renderExperimentalLabel = (label: string) => (
    <Group gap="xs" align="center" wrap="nowrap">
      <Text size="sm">{label}</Text>
      <Tooltip label="Experimental" withArrow>
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

  return (
    <>
      <Modal
        zIndex={300}
        opened={opened}
        onClose={onClose}
        title="Settings"
        yOffset="120px"
        size="lg"
      >
        <Stack gap="md">
          <Group align="flex-end" gap="sm">
            <TextInput
              label="osu! Songs Folder"
              style={{ flexGrow: 1 }}
              value={songFolder}
              readOnly
              onClick={() => songFolder === '' && pickFolder()}
            />
            <Button leftSection={<IconFolder size={18} />} variant="light" onClick={pickFolder}>
              Browse
            </Button>
          </Group>
          <Select
            label="Font"
            data={UI_FONT_FAMILY_OPTIONS}
            value={uiFontFamily}
            allowDeselect={false}
            onChange={(value) => {
              const font = parseUiFontFamily(value ?? DEFAULT_UI_FONT_FAMILY);
              setUiFontFamily(font);
              setSettings((prev) => ({ ...prev, uiFontFamily: font }));
            }}
          />
          <Group align="center" gap="sm" justify="space-between" wrap="nowrap">
            <Switch
              style={{ flex: 1, minWidth: 0 }}
              label={
                <Group gap="xs" align="center" wrap="nowrap">
                  <MinorIcon size={16} />
                  Show minor issues
                </Group>
              }
              checked={showMinor}
              onChange={(e) => {
                const checked = e.currentTarget.checked;
                setShowMinor(checked);
                setSettings((prev) => ({ ...prev, showMinor: checked }));
              }}
            />
            {showMinor && (
              <Tooltip label="Loading check catalogue…" disabled={docsStatus === 'success'}>
                <Box style={{ flexShrink: 0 }}>
                  <Button
                    size="compact-xs"
                    variant="light"
                    disabled={docsStatus !== 'success'}
                    onClick={openMinorFilterModal}
                  >
                    Filter minor checks
                  </Button>
                </Box>
              </Tooltip>
            )}
          </Group>
          <Switch
            label="Use difficulty names from corresponding game modes"
            checked={showGamemodeDifficultyNames}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              setShowGamemodeDifficultyNames(checked);
              setSettings((prev) => ({ ...prev, showGamemodeDifficultyNames: checked }));
            }}
          />
          <Switch
            label="Go to checks tab when switching mapsets"
            checked={goToChecksOnMapsetSwitch}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              setGoToChecksOnMapsetSwitch(checked);
              setSettings((prev) => ({ ...prev, goToChecksOnMapsetSwitch: checked }));
            }}
          />
          <Switch
            label="Show check changes since last run"
            checked={showCheckRunDelta}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              setShowCheckRunDelta(checked);
              setSettings((prev) => ({ ...prev, showCheckRunDelta: checked }));
            }}
          />
          {showCheckRunDelta && (
            <Switch
              label="Include unchanged issues in check delta"
              checked={checkRunDeltaShowUnchanged}
              onChange={(e) => {
                const checked = e.currentTarget.checked;
                setCheckRunDeltaShowUnchanged(checked);
                setSettings((prev) => ({ ...prev, checkRunDeltaShowUnchanged: checked }));
              }}
            />
          )}
          <Switch
            label={renderExperimentalLabel('Show advanced audio analysis')}
            checked={showAdvancedAudioAnalysis}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              if (checked === showAdvancedAudioAnalysis) {
                return;
              }

              if (!checked) {
                setShowAdvancedAudioAnalysis(false);
                setSettings((prev) => ({ ...prev, showAdvancedAudioAnalysis: false }));
                return;
              }

              if (!showAdvancedAudioAnalysis) {
                setAdvancedAudioConfirmOpened(true);
              }
            }}
          />
          <Switch
            label={renderExperimentalLabel('osu!(lazer) support')}
            checked={lazerLookupEnabled}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              if (!checked) {
                setLazerLookupEnabled(false);
                setSettings((prev) => ({ ...prev, lazerLookupEnabled: false }));
                return;
              }
              if (!lazerLookupEnabled) {
                setLazerWarningOpened(true);
              }
            }}
          />
          <Divider my="xs" />
          <PluginManager opened={opened} />
          <Divider my="xs" />
          <Group justify="space-between" align="end">
            <div>
              <Text fw={500}>Application updates</Text>
              <Text size="sm" c="dimmed">
                Current version: {currentVersion}
              </Text>
            </div>
            <Button
              leftSection={<IconRefresh size={18} />}
              variant="light"
              onClick={() => void openUpdater()}
            >
              Check for updates
            </Button>
          </Group>
          <Switch
            label="Receive beta updates"
            description={
              receivePrereleases
                ? 'Includes beta releases like 2.0.0-beta.1 when available.'
                : currentVersionIsPrerelease
                  ? 'Only stable releases will be offered. Use Check for updates to return to stable when one is available.'
                  : 'Only stable releases will be offered.'
            }
            checked={receivePrereleases}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              setReceivePrereleases(checked);
              setSettings((prev) => ({ ...prev, receivePrereleases: checked }));
              void checkForUpdates({
                silent: false,
                openModal: true,
                allowPrereleaseOverride: checked,
              });
            }}
          />
          <Divider my="xs" />
          <Button.Group w="100%">
            <Button
              variant="default"
              flex={1}
              leftSection={<IconFolder size={18} />}
              onClick={() => void openAppFolder()}
            >
              Open app folder
            </Button>
            <Button
              variant="default"
              flex={1}
              leftSection={<IconFolder size={18} />}
              onClick={() => void openExternalsFolder()}
            >
              Open externals folder
            </Button>
          </Button.Group>
          <Button.Group w="100%">
            <Button
              variant="default"
              flex={1}
              leftSection={<IconBrandGithub size={18} />}
              onClick={() => void openExternal(SOURCE_CODE_URL)}
            >
              Source code
            </Button>
            <Button
              variant="default"
              flex={1}
              leftSection={<IconWorld size={18} />}
              onClick={() => void openExternal(WEBSITE_URL)}
            >
              Website
            </Button>
          </Button.Group>
          {isDev && (
            <>
              <Divider my="sm" />
              <Stack gap="sm">
                <Text size="sm" c="dimmed">
                  The following options affect development mode only and have no effect in
                  production builds.
                </Text>
                <Switch
                  label="Gate backend in DEV (start sidecar port 5005)"
                  checked={gateInDev}
                  onChange={(e) => {
                    const checked = e.currentTarget.checked;
                    setGateInDev(checked);
                    setSettings((prev) => ({ ...prev, gateInDev: checked }));
                  }}
                />
                <Alert icon={<IconNote />} title="Note" color="yellow" variant="light">
                  <Group gap="sm">
                    <Text size="sm">
                      Enabling this option will make the app mimic production mode by running the
                      sidecar.
                    </Text>
                    <Text size="sm">
                      This does need the sidecar to be built beforehand and available in the
                      following folder: <code>/bin/server/dist/&lt;rid&gt;/</code>
                    </Text>
                    <Text size="sm">
                      Changing this settings may require restarting the application to take effect.
                    </Text>
                  </Group>
                </Alert>
              </Stack>
            </>
          )}
        </Stack>
      </Modal>
      <LazerLookupWarningModal
        opened={lazerWarningOpened}
        onCancel={() => setLazerWarningOpened(false)}
        onConfirm={() => {
          setLazerLookupEnabled(true);
          setSettings((prev) => ({ ...prev, lazerLookupEnabled: true }));
          setLazerWarningOpened(false);
        }}
      />
      <AdvancedAudioWarningModal
        opened={advancedAudioConfirmOpened}
        onCancel={() => {
          setAdvancedAudioConfirmOpened(false);
        }}
        onConfirm={() => {
          setShowAdvancedAudioAnalysis(true);
          setSettings((prev) => ({ ...prev, showAdvancedAudioAnalysis: true }));
          setAdvancedAudioConfirmOpened(false);
        }}
      />
      <MinorChecksFilterModal opened={minorFilterOpened} onClose={closeMinorFilterModal} />
    </>
  );
};

export default SettingsModal;
