import { Button, SimpleGrid } from '@mantine/core';
import { IconBrandGithub, IconFolder, IconInfoCircle, IconWorld } from '@tabler/icons-react';
import { SettingsSection } from './SettingsSection';
import { SOURCE_CODE_URL, WEBSITE_URL } from '../../Constants';
import { useOpenExternal } from '../../hooks/useOpenExternal';

export default function AboutSettingsSection() {
  const openExternal = useOpenExternal();

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

  const openAppFolder = () =>
    openFolder(() => window.electronAPI?.app.getAppFolderPath() ?? Promise.resolve(undefined));

  const openExternalsFolder = () =>
    openFolder(
      () => window.electronAPI?.app.getExternalsFolderPath() ?? Promise.resolve(undefined)
    );

  return (
    <SettingsSection
      icon={<IconInfoCircle size={28} />}
      title="About and folders"
      description="Quick access to application folders and project links."
    >
      <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
        <Button
          size="sm"
          variant="light"
          leftSection={<IconFolder size={18} />}
          onClick={() => void openAppFolder()}
        >
          Open app folder
        </Button>
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
          leftSection={<IconBrandGithub size={18} />}
          onClick={() => void openExternal(SOURCE_CODE_URL)}
        >
          Source code
        </Button>
        <Button
          size="sm"
          variant="light"
          leftSection={<IconWorld size={18} />}
          onClick={() => void openExternal(WEBSITE_URL)}
        >
          Website
        </Button>
      </SimpleGrid>
    </SettingsSection>
  );
}
