import {
  Badge,
  Box,
  Button,
  Group,
  Paper,
  ScrollArea,
  Stack,
  Switch,
  Text,
  Tooltip,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { IconAdjustments } from '@tabler/icons-react';
import { useMemo } from 'react';
import MinorChecksFilterModal from './MinorChecksFilterModal';
import { SettingsRow, SettingsSection } from './SettingsSection';
import { useDocumentation } from '../../context/DocumentationContext';
import { useSettings } from '../../context/SettingsContext';
import { dedupeDocumentationChecksById } from '../documentation/filterDocumentationChecks';
import { useDocumentationChecks } from '../documentation/hooks/useDocumentationChecks';
import MinorIcon from '../icons/MinorIcon';

export default function CheckSettingsSection() {
  const { settings, setSettings } = useSettings();
  const { status: docsStatus } = useDocumentation();
  const { allChecks } = useDocumentationChecks();
  const [minorFilterOpened, { open: openMinorFilterModal, close: closeMinorFilterModal }] =
    useDisclosure(false);

  const minorChecks = useMemo(
    () =>
      dedupeDocumentationChecksById(allChecks.filter((check) => check.outcomes.includes('Minor'))),
    [allChecks]
  );

  const hiddenMinorChecks = useMemo(() => {
    const checksById = new Map(minorChecks.map((check) => [check.id, check]));
    return settings.hiddenMinorCheckIds.map((id) => ({
      id,
      check: checksById.get(id),
    }));
  }, [minorChecks, settings.hiddenMinorCheckIds]);

  return (
    <>
      <SettingsSection
        icon={<IconAdjustments size={28} />}
        title="Checks"
        description="Controls how check results are displayed and how check runs are compared."
      >
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
              onChange={(e) =>
                setSettings((prev) => ({ ...prev, showMinor: e.currentTarget.checked }))
              }
            />
          }
        />
        {settings.showMinor && (
          <Stack gap="xs">
            <SettingsRow
              title="Negligible check filter"
              description={`${hiddenMinorChecks.length} of ${minorChecks.length} checks disabled.`}
              control={
                <Tooltip label="Loading check catalogue..." disabled={docsStatus === 'success'}>
                  <Box>
                    <Button
                      size="sm"
                      variant="light"
                      disabled={docsStatus !== 'success'}
                      onClick={openMinorFilterModal}
                    >
                      Disable negligible checks
                    </Button>
                  </Box>
                </Tooltip>
              }
            />
            <Paper withBorder radius="sm" p="sm">
              {hiddenMinorChecks.length === 0 ? (
                <Text size="sm" c="dimmed">
                  No negligible checks are currently disabled.
                </Text>
              ) : (
                <ScrollArea.Autosize mah={180} offsetScrollbars type="auto" scrollbars="y">
                  <Stack gap="xs">
                    {hiddenMinorChecks.map(({ id, check }) => (
                      <Group key={id} gap="xs" wrap="nowrap" align="flex-start">
                        <Badge size="xs" variant="light" color="gray">
                          #{id}
                        </Badge>
                        <Stack gap={2} style={{ minWidth: 0 }}>
                          <Text size="sm" lineClamp={2}>
                            {check?.description ?? 'Unknown check'}
                          </Text>
                          {check?.category && (
                            <Text size="xs" c="dimmed">
                              {check.category}
                            </Text>
                          )}
                        </Stack>
                      </Group>
                    ))}
                  </Stack>
                </ScrollArea.Autosize>
              )}
            </Paper>
          </Stack>
        )}
        <SettingsRow
          title="Use difficulty names from corresponding game modes"
          description="Shows mode-specific difficulty names where available."
          control={
            <Switch
              checked={settings.showGamemodeDifficultyNames}
              onChange={(e) =>
                setSettings((prev) => ({
                  ...prev,
                  showGamemodeDifficultyNames: e.currentTarget.checked,
                }))
              }
            />
          }
        />
        <SettingsRow
          title="Go to checks tab when switching mapsets"
          description="Automatically opens the checks page after selecting another mapset."
          control={
            <Switch
              checked={settings.goToChecksOnMapsetSwitch}
              onChange={(e) =>
                setSettings((prev) => ({
                  ...prev,
                  goToChecksOnMapsetSwitch: e.currentTarget.checked,
                }))
              }
            />
          }
        />
        <SettingsRow
          title="Automatically create a snapshot when checks are run"
          description="Stores a snapshot before each check run so the Snapshots page can compare changes over time."
          control={
            <Switch
              checked={settings.autoCreateSnapshotOnCheckRun}
              onChange={(e) =>
                setSettings((prev) => ({
                  ...prev,
                  autoCreateSnapshotOnCheckRun: e.currentTarget.checked,
                }))
              }
            />
          }
        />
        <SettingsRow
          title="Show check changes since last run"
          description="Adds the delta summary to the checks page."
          control={
            <Switch
              checked={settings.showCheckRunDelta}
              onChange={(e) =>
                setSettings((prev) => ({ ...prev, showCheckRunDelta: e.currentTarget.checked }))
              }
            />
          }
        />
        {settings.showCheckRunDelta && (
          <SettingsRow
            title="Include unchanged issues in check delta"
            description="Shows unchanged findings alongside added and resolved findings."
            control={
              <Switch
                checked={settings.checkRunDeltaShowUnchanged}
                onChange={(e) =>
                  setSettings((prev) => ({
                    ...prev,
                    checkRunDeltaShowUnchanged: e.currentTarget.checked,
                  }))
                }
              />
            }
          />
        )}
      </SettingsSection>
      <MinorChecksFilterModal opened={minorFilterOpened} onClose={closeMinorFilterModal} />
    </>
  );
}
