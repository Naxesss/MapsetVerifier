import {
  Alert,
  Box,
  Button,
  Center,
  Divider,
  Group,
  Loader,
  Modal,
  Progress,
  Stack,
  Text,
} from '@mantine/core';
import { IconAlertCircle, IconCircleCheck, IconCloudDownload } from '@tabler/icons-react';
import React, { useMemo } from 'react';
import TurndownService from 'turndown';
import { useUpdater } from '../../context/UpdaterContext';
import { isSemverPreRelease } from '../../utils/isSemverPreRelease';
import MantineMarkdown from '../documentation/MantineMarkdown';

const releaseNotesTurndown = new TurndownService({
  headingStyle: 'atx',
  bulletListMarker: '-',
  codeBlockStyle: 'fenced',
});

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
    status,
    currentVersionIsPrerelease,
    receivePrereleases,
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
  const releaseNotesMarkdown = useMemo(
    () => (updateNotes ? releaseNotesTurndown.turndown(updateNotes) : ''),
    [updateNotes],
  );
  const availableUpdateIsPrerelease = isSemverPreRelease(availableUpdate?.version);

  const upToDateTitle =
    !receivePrereleases && currentVersionIsPrerelease
      ? 'No stable update available'
      : 'You are up to date';
  const upToDateMessage =
    !receivePrereleases && currentVersionIsPrerelease
      ? 'You are currently on a beta build. No stable release is available yet.'
      : receivePrereleases
        ? 'No newer release was found for this installation.'
        : 'No newer stable release was found for this installation.';
  const availableTitle = availableUpdateIsPrerelease
    ? `Beta ${availableUpdate?.version} is available`
    : `Update ${availableUpdate?.version} is available`;

  const dismissLabel =
    status === 'available' ? 'Later' : status === 'installed' ? 'Close' : 'Cancel';

  return (
    <Modal.Root
      opened={opened}
      onClose={closeUpdater}
      size="lg"
      centered
      zIndex={400}
      closeOnClickOutside={!busy}
      closeOnEscape={!busy}
    >
      <Modal.Overlay />
      <Modal.Content
        styles={{
          content: {
            display: 'flex',
            flexDirection: 'column',
            overflow: 'hidden',
          },
        }}
      >
        <Modal.Header>
          <Modal.Title>Application updates</Modal.Title>
          {!busy && <Modal.CloseButton />}
        </Modal.Header>

        <Modal.Body
          styles={{
            body: {
              flex: 1,
              minHeight: 0,
              overflow: 'auto',
            },
          }}
        >
          <Stack gap="md">
            {status === 'checking' && (
              <Center py="xl">
                <Stack align="center" gap="md">
                  <Loader
                    size="lg"
                    style={{
                      opacity: 0.22,
                      color: 'var(--mantine-color-primary-2)',
                    }}
                  />
                  <Text fw={600} ta="center">
                    Checking GitHub Releases for a newer version…
                  </Text>
                </Stack>
              </Center>
            )}

            {status === 'up-to-date' && (
              <Alert icon={<IconCircleCheck />} color="green" title={upToDateTitle}>
                {upToDateMessage}
              </Alert>
            )}

            {(status === 'available' || status === 'downloading' || status === 'installing') &&
              availableUpdate && (
                <Alert icon={<IconCloudDownload />} color="blue" title={availableTitle}>
                  {status === 'available'
                    ? 'A new version is available. Would you like to update now?'
                    : 'The update package is being downloaded and installed.'}
                </Alert>
              )}

            {status === 'installed' && (
              <Alert
                icon={<IconCircleCheck />}
                color="green"
                title={`Update ${completedVersion ?? 'installed'}`}
              >
                The update has been installed. On Windows, the app may close automatically while
                the installer finishes.
              </Alert>
            )}

            {status === 'error' && errorMessage && (
              <Alert icon={<IconAlertCircle />} color="red" title="Updater error">
                {errorMessage}
              </Alert>
            )}

            {availableUpdate && status !== 'checking' && (
              <>
                <Stack gap="xs">
                  <Text fw={500}>Release details</Text>
                  <Text size="sm">Target version: {availableUpdate.version}</Text>
                  {availableUpdate.date && (
                    <Text size="sm" c="dimmed">
                      Published: {new Date(availableUpdate.date).toLocaleString()}
                    </Text>
                  )}
                </Stack>

                {updateNotes && (
                  <>
                    <Divider />
                    <Text size="sm" fw={500}>
                      Changelog
                    </Text>
                    <MantineMarkdown>{releaseNotesMarkdown}</MantineMarkdown>
                  </>
                )}
              </>
            )}

          </Stack>
        </Modal.Body>

        <Box px="md" pb="md" style={{ flexShrink: 0 }}>
          {status !== 'checking' && <Divider mb="md" />}
          {(status === 'downloading' || status === 'installing') ? (
            <Stack gap="xs">
              <Progress value={progress} animated={status !== 'installing'} />
              <Group justify="space-between">
                <Text size="sm">
                  {status === 'installing' ? 'Installing update…' : 'Downloading update…'}
                </Text>
                <Text size="sm" c="dimmed">
                  {formatBytes(downloadedBytes)} / {formatBytes(totalBytes)}
                </Text>
              </Group>
            </Stack>
          ) : (
            <Group justify="flex-end">
              {(status === 'up-to-date' || status === 'error' || status === 'installed') && (
                <Button
                  variant="light"
                  onClick={() => void checkForUpdates({ silent: false, openModal: true })}
                >
                  Check again
                </Button>
              )}

              {status === 'available' && availableUpdate && (
                <Button variant="light" onClick={() => void installUpdate()}>
                  Update now
                </Button>
              )}

              {!busy && (
                <Button variant="default" onClick={closeUpdater}>
                  {dismissLabel}
                </Button>
              )}
            </Group>
          )}
        </Box>
      </Modal.Content>
    </Modal.Root>
  );
};

export default UpdaterModal;
