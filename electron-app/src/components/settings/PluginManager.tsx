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
  SimpleGrid,
  Stack,
  Switch,
  Table,
  Text,
} from '@mantine/core';
import {
  IconAlertTriangle,
  IconBook,
  IconFolder,
  IconInfoCircle,
  IconRefresh,
} from '@tabler/icons-react';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import PluginApi from '../../client/PluginApi.ts';
import { CUSTOM_CHECKS_DOCS_URL } from '../../Constants.ts';
import { useDocumentation } from '../../context/DocumentationContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';
import { useOpenExternal } from '../../hooks/useOpenExternal';
import { ApiPluginReport } from '../../Types.ts';

interface PluginManagerProps {
  opened: boolean;
}

const PluginManager: React.FC<PluginManagerProps> = ({ opened }) => {
  const { reload: reloadDocumentation } = useDocumentation();
  const { settings, setSettings } = useSettings();
  const openExternal = useOpenExternal();
  const [report, setReport] = useState<ApiPluginReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const openFolder = async (getPath: () => Promise<string | undefined>) => {
    try {
      const folderPath = await getPath();
      if (!folderPath) return;
      const err = await window.electronAPI?.shell.openPath(folderPath);
      if (err) throw new Error(err);
    } catch (e) {
      console.error('[Settings] Failed to open folder:', e);
      alert('Failed to open folder. See console for details.');
    }
  };

  const openExternalsFolder = () =>
    openFolder(
      () => window.electronAPI?.app.getExternalsFolderPath() ?? Promise.resolve(undefined)
    );

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

  const reloadPlugins = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      setReport(await PluginApi.reloadPlugins(settings.customChecksEnabled));
      await reloadDocumentation();
    } catch (e: any) {
      console.error('[PluginManager] Failed to reload plugins:', e);
      setError(e?.message ?? 'Failed to reload plugins.');
    } finally {
      setLoading(false);
    }
  }, [reloadDocumentation, settings.customChecksEnabled]);

  const setCustomChecksEnabled = useCallback(
    async (customChecksEnabled: boolean) => {
      setLoading(true);
      setError(null);

      try {
        setReport(await PluginApi.reloadPlugins(customChecksEnabled));
        setSettings((prev) => ({ ...prev, customChecksEnabled }));
        await reloadDocumentation();
      } catch (e: any) {
        console.error('[PluginManager] Failed to toggle custom checks:', e);
        setError(e?.message ?? 'Failed to toggle custom checks.');
      } finally {
        setLoading(false);
      }
    },
    [reloadDocumentation, setSettings]
  );

  useEffect(() => {
    if (!opened) return;

    let cancelled = false;

    void Promise.resolve().then(() => {
      if (!cancelled) void loadPlugins();
    });

    return () => {
      cancelled = true;
    };
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
      <Alert icon={<IconInfoCircle />} title="Installing plugins" color="blue" variant="light">
        <List size="sm" type="ordered">
          <List.Item>Download the DLL of the plugin you want to use.</List.Item>
          <List.Item>
            Put the DLL in the <Code>CustomChecks</Code> folder. Removing or replacing an already
            loaded plugin is not possible without shutting down this app first.
          </List.Item>
          <List.Item>
            Use the <Code>Reload checks</Code> button to load newly installed plugins, or restart
            the application.
          </List.Item>
        </List>
      </Alert>
      <Alert
        icon={<IconAlertTriangle />}
        title="Plugins built for MapsetVerifier V1"
        color="orange"
        variant="light"
      >
        Plugins built against the old MapsetVerifier V1 API are not compatible with this version and
        will fail to load. See the custom checks documentation for the current plugin API and how to
        update an existing plugin.
      </Alert>
      <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="sm">
        <Button
          size="sm"
          variant="light"
          leftSection={<IconFolder size={18} />}
          onClick={() => void openExternalsFolder()}
        >
          Open externals folder
        </Button>
        <Button
          size="sm"
          variant="light"
          leftSection={loading ? <Loader size={18} /> : <IconRefresh size={18} />}
          onClick={() => void reloadPlugins()}
          disabled={loading}
        >
          Reload checks
        </Button>
        <Button
          size="sm"
          variant="light"
          leftSection={<IconBook size={18} />}
          onClick={() => void openExternal(CUSTOM_CHECKS_DOCS_URL)}
        >
          Custom checks docs
        </Button>
      </SimpleGrid>
      <Group justify="space-between" align="center" wrap="nowrap">
        <Stack gap={0}>
          <Text size="sm" fw={500}>
            Load custom checks
          </Text>
          <Text size="xs" c="dimmed">
            Reloads checks immediately when changed.
          </Text>
        </Stack>
        <Switch
          checked={settings.customChecksEnabled}
          onChange={(e) => void setCustomChecksEnabled(e.currentTarget.checked)}
          disabled={loading}
        />
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
          <Text size="sm" c="dimmed" truncate>
            Folder: {report?.directoryPath || 'Not configured'}
          </Text>
        </Stack>
      </Paper>

      {error && (
        <Alert icon={<IconAlertTriangle size={16} />} color="red" variant="light">
          {error}
        </Alert>
      )}

      {report && report.loadedPlugins.length === 0 && report.failedPlugins.length === 0 && (
        <Alert color="gray" variant="light">
          {settings.customChecksEnabled
            ? 'No custom check DLLs were found. Add plugins to the CustomChecks folder and reload checks.'
            : 'Custom checks are disabled. Built-in checks remain active.'}
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
                        <List.Item key={checkName}>{checkName}</List.Item>
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
                <Text c="orange">{plugin.fileName}</Text>
              </Accordion.Control>
              <Accordion.Panel>
                <Stack gap="xs">
                  <Text size="sm" c="red">
                    {plugin.message}
                  </Text>
                  <Code block fz="sm" style={{ wordBreak: 'break-word', whiteSpace: 'pre-wrap' }}>
                    {plugin.details}
                  </Code>
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
