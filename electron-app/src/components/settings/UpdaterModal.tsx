import { Alert, Button, Divider, Group, Modal, Progress, ScrollArea, Stack, Text } from '@mantine/core';
import { IconAlertCircle, IconCircleCheck, IconCloudDownload, IconLoader2 } from '@tabler/icons-react';
import React from 'react';
import { useUpdater } from '../../context/UpdaterContext';
import MantineMarkdown from '../documentation/MantineMarkdown';

function formatBytes(value?: number | null) {
  if (value == null || value <= 0) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB'];
  let size = value;
  let unitIndex = 0;

  while (size >= 1024 && unitIndex < units.length - 1) {
    size /= 1024;
    unitIndex += 1;
  }

  return `${size.toFixed(size >= 10 || unitIndex === 0 ? 0 : 1)} ${units[unitIndex]}`;
}

const UpdaterModal: React.FC = () => {
  const {
    opened,
    currentVersion,
    status,
    availableUpdate,
    errorMessage,
    downloadedBytes,
    totalBytes,
    progress,
    completedVersion,
    checkForUpdates,
    closeUpdater,
    installUpdate,
  } = useUpdater();
  const busy = status === 'checking' || status === 'downloading' || status === 'installing';

  const updateNotes = availableUpdate?.body?.trim();

  return (
    <Modal
      opened={opened}
      onClose={busy ? () => undefined : closeUpdater}
      title="Application updates"
      yOffset="120px"
      size="lg"
      closeOnClickOutside={!busy}
      closeOnEscape={!busy}
      withCloseButton={!busy}
    >
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          Installed version: {currentVersion}
        </Text>

        {status === 'checking' && (
          <Alert icon={<IconLoader2 />} color="blue">
            Checking GitHub Releases for a newer version…
          </Alert>
        )}

        {status === 'up-to-date' && (
          <Alert icon={<IconCircleCheck />} color="green" title="You are up to date">
            No newer release was found for this installation.
          </Alert>
        )}

        {(status === 'available' || status === 'downloading' || status === 'installing') && availableUpdate && (
          <Alert icon={<IconCloudDownload />} color="blue" title={`Update ${availableUpdate.version} is available`}>
            {status === 'available'
              ? 'A new version is available. Would you like to update now?'
              : 'The update package is being downloaded and installed.'}
          </Alert>
        )}

        {status === 'installed' && (
          <Alert icon={<IconCircleCheck />} color="green" title={`Update ${completedVersion ?? 'installed'}`}>
            The update has been installed. On Windows, the app may close automatically while the installer finishes.
          </Alert>
        )}

        {status === 'error' && errorMessage && (
          <Alert icon={<IconAlertCircle />} color="red" title="Updater error">
            {errorMessage}
          </Alert>
        )}

        {availableUpdate && (
          <Stack gap="xs">
            <Text fw={500}>Release details</Text>
            <Text size="sm">Target version: {availableUpdate.version}</Text>
            {availableUpdate.date && (
              <Text size="sm" c="dimmed">
                Published: {new Date(availableUpdate.date).toLocaleString()}
              </Text>
            )}
            {updateNotes && (
              <>
                <Divider my="xs" />
                <ScrollArea mah={220} type="auto" offsetScrollbars>
                  <MantineMarkdown>{updateNotes}</MantineMarkdown>
                </ScrollArea>
              </>
            )}
          </Stack>
        )}

        {(status === 'downloading' || status === 'installing') && (
          <Stack gap="xs">
            <Progress value={progress} animated={status !== 'installing'} />
            <Group justify="space-between">
              <Text size="sm">{status === 'installing' ? 'Installing update…' : 'Downloading update…'}</Text>
              <Text size="sm" c="dimmed">
                {formatBytes(downloadedBytes)} / {formatBytes(totalBytes)}
              </Text>
            </Group>
          </Stack>
        )}

        <Group justify="flex-end">
          {(status === 'up-to-date' || status === 'error' || status === 'installed') && (
            <Button variant="light" onClick={() => void checkForUpdates({ silent: false, openModal: true })}>
              Check again
            </Button>
          )}

          {status === 'available' && availableUpdate && (
            <Button onClick={() => void installUpdate()}>Update now</Button>
          )}

          {!busy && (
            <Button variant="default" onClick={closeUpdater}>
              {status === 'available' ? 'Later' : status === 'installed' ? 'Close' : 'Cancel'}
            </Button>
          )}
        </Group>
      </Stack>
    </Modal>
  );
};

export default UpdaterModal;
