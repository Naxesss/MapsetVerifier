import {
  Accordion,
  Alert,
  Badge,
  Button,
  Code,
  Group,
  List,
  Loader,
  Paper,
  Stack,
  Table,
  Text,
  Tooltip,
} from '@mantine/core';
import { IconAlertTriangle, IconPackage, IconRefresh } from '@tabler/icons-react';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import PluginApi from '../../client/PluginApi.ts';
import { ApiPluginReport } from '../../Types.ts';

interface PluginManagerProps {
  opened: boolean;
}

const PluginManager: React.FC<PluginManagerProps> = ({ opened }) => {
  const [report, setReport] = useState<ApiPluginReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadPlugins = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      setReport(await PluginApi.getPlugins());
    } catch (e: any) {
      console.error('[PluginManager] Failed to load plugins:', e);
      setError(e?.message ?? 'Failed to load plugin status.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!opened) return;

    void loadPlugins();
  }, [loadPlugins, opened]);

  const totals = useMemo(() => {
    const loaded = report?.loadedPlugins ?? [];

    return {
      pluginCount: loaded.length,
      checkCount: loaded.reduce((sum, plugin) => sum + plugin.checkCount, 0),
      failedCount: report?.failedPlugins.length ?? 0,
    };
  }, [report]);

  return (
    <Stack gap="sm">
      <Group justify="space-between" align="center">
        <Group gap="xs">
          <IconPackage size={18} />
          <Text fw={500}>Plugin manager</Text>
        </Group>
        <Button
          size="compact-sm"
          variant="light"
          leftSection={loading ? <Loader size={14} /> : <IconRefresh size={15} />}
          onClick={() => void loadPlugins()}
          disabled={loading}
        >
          Refresh
        </Button>
      </Group>

      <Paper withBorder p="sm" radius="sm">
        <Stack gap="xs">
          <Group gap="xs">
            <Badge variant="light" color="green">
              {totals.pluginCount} loaded
            </Badge>
            <Badge variant="light" color={totals.failedCount > 0 ? 'red' : 'gray'}>
              {totals.failedCount} failed
            </Badge>
            <Badge variant="light" color="blue">
              {totals.checkCount} checks
            </Badge>
          </Group>
          <Tooltip label={report?.directoryPath ?? ''} disabled={!report?.directoryPath}>
            <Text size="sm" c="dimmed" truncate>
              Folder: {report?.directoryPath || 'Not configured'}
            </Text>
          </Tooltip>
        </Stack>
      </Paper>

      {error && (
        <Alert icon={<IconAlertTriangle size={16} />} color="red" variant="light">
          {error}
        </Alert>
      )}

      {report && report.loadedPlugins.length === 0 && report.failedPlugins.length === 0 && (
        <Alert color="gray" variant="light">
          No custom check DLLs were found. Add plugins to the CustomChecks folder and restart the
          backend.
        </Alert>
      )}

      {report && report.loadedPlugins.length > 0 && (
        <Table verticalSpacing="xs">
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Plugin</Table.Th>
              <Table.Th>Version</Table.Th>
              <Table.Th>Author</Table.Th>
              <Table.Th>Checks</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {report.loadedPlugins.map((plugin) => (
              <Table.Tr key={plugin.filePath}>
                <Table.Td>
                  <Stack gap={2}>
                    <Text size="sm" fw={500}>
                      {plugin.assemblyName}
                    </Text>
                    <Text size="xs" c="dimmed" truncate maw={220}>
                      {plugin.fileName}
                    </Text>
                  </Stack>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{plugin.version ?? 'Unknown'}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">
                    {plugin.authors.length > 0 ? plugin.authors.join(', ') : 'Unknown'}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Group gap={4}>
                    <Badge size="sm" variant="light">
                      {plugin.checkCount} total
                    </Badge>
                    {plugin.generalCheckCount > 0 && (
                      <Badge size="sm" variant="outline">
                        {plugin.generalCheckCount} general
                      </Badge>
                    )}
                    {plugin.beatmapCheckCount > 0 && (
                      <Badge size="sm" variant="outline">
                        {plugin.beatmapCheckCount} beatmap
                      </Badge>
                    )}
                    {plugin.beatmapSetCheckCount > 0 && (
                      <Badge size="sm" variant="outline">
                        {plugin.beatmapSetCheckCount} set
                      </Badge>
                    )}
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      )}

      {report && report.loadedPlugins.some((plugin) => plugin.checkNames.length > 0) && (
        <Accordion variant="contained">
          {report.loadedPlugins
            .filter((plugin) => plugin.checkNames.length > 0)
            .map((plugin) => (
              <Accordion.Item key={plugin.filePath} value={plugin.filePath}>
                <Accordion.Control>{plugin.assemblyName}</Accordion.Control>
                <Accordion.Panel>
                  <Stack gap={4}>
                    <List>
                    {plugin.checkNames.map((checkName) => (
                      <List.Item key={checkName}>
                        {checkName}
                      </List.Item>
                    ))}
                    </List>
                  </Stack>
                </Accordion.Panel>
              </Accordion.Item>
            ))}
        </Accordion>
      )}

      {report && report.failedPlugins.length > 0 && (
        <Accordion variant="contained">
          {report.failedPlugins.map((plugin) => (
            <Accordion.Item key={plugin.filePath} value={plugin.filePath}>
              <Accordion.Control icon={<IconAlertTriangle size={16} color="orange" />}>
                <Text c="orange">
                  {plugin.fileName}
                </Text>
              </Accordion.Control>
              <Accordion.Panel>
                <Stack gap="xs">
                  <Text size="sm" c="red">
                    {plugin.message}
                  </Text>
                  <Code
                    block
                    fz="sm"
                    style={{ wordBreak: 'break-word', whiteSpace: 'pre-wrap' }}
                  >{plugin.details}</Code>
                </Stack>
              </Accordion.Panel>
            </Accordion.Item>
          ))}
        </Accordion>
      )}
    </Stack>
  );
};

export default PluginManager;
