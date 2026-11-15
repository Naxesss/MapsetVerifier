import {
  Box,
  Group,
  Image,
  Modal,
  SimpleGrid,
  Stack,
  Table,
  Text,
  UnstyledButton,
} from '@mantine/core';
import { useElementSize } from '@mantine/hooks';
import {
  IconArrowDown,
  IconArrowUp,
  IconChevronDown,
  IconChevronRight,
  IconPhotoOff,
} from '@tabler/icons-react';
import { useMemo, useState } from 'react';
import { buildBeatmapResourceUrl } from '../../../../utils/buildBeatmapFolderPath.ts';
import { countWord } from '../../../../utils/countWord.ts';
import AppTable from '../../../common/AppTable.tsx';
import EmptyState from '../../../common/EmptyState.tsx';
import SectionCard from '../../../common/SectionCard.tsx';
import { StatField } from '../../../common/StatField.tsx';
import type { StoryboardImage } from '../../../../Types';

interface StoryboardImagesProps {
  images: StoryboardImage[];
  beatmapFolderPath: string | undefined;
}

type SortKey = 'name' | 'resolution' | 'size' | 'uses';
type Sort = { key: SortKey; descending: boolean };

/** Up to this many images, every folder starts open; past it, they start closed. */
const OPEN_ALL_UP_TO = 40;
const THUMBNAIL_SIZE = 40;

interface Folder {
  /**
   * The subfolder's path with a trailing slash, e.g. "sb/lyrics/"; empty for the images that
   * aren't grouped, those in the mapset folder or a top folder like "sb/".
   */
  path: string;
  images: StoryboardImage[];
  sizeBytes: number;
  missingCount: number;
}

/**
 * The group an image belongs to: its second-level folder ("sb/scene1/" for "sb/scene1/fx/a.png"),
 * or empty when it sits in the mapset folder or a top folder like "sb/".
 */
function groupOf(path: string) {
  const folders = path.split('/').slice(0, -1);
  return folders.length >= 2 ? `${folders[0]}/${folders[1]}/` : '';
}

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(2)} MB`;
}

const nameOf = (path: string) => path.slice(path.lastIndexOf('/') + 1);

/** Every missing file's path, for a tooltip. */
const missingTitle = (image: StoryboardImage) =>
  image.missingFiles.length > 0 ? `Missing: ${image.missingFiles.join(', ')}` : undefined;

/** The files the storyboard asks for that aren't in the mapset folder, by path. */
function MissingFiles({ image }: { image: StoryboardImage }) {
  if (image.missingFiles.length === 0) return null;
  return (
    <StatField
      label={image.isAnimation ? 'Missing frames' : 'Missing file'}
      value={
        <Stack gap={0}>
          {image.missingFiles.map((file) => (
            <Text key={file} size="sm" c="orange">
              {file}
            </Text>
          ))}
        </Stack>
      }
      note="Not in the mapset folder at this path."
    />
  );
}

function compare(a: StoryboardImage, b: StoryboardImage, key: SortKey) {
  switch (key) {
    case 'name':
      return a.path.localeCompare(b.path, undefined, { numeric: true });
    case 'resolution':
      return a.width * a.height - b.width * b.height;
    case 'size':
      return a.fileSizeBytes - b.fileSizeBytes;
    case 'uses':
      return a.useCount - b.useCount;
  }
}

/**
 * Images of subfolders like "sb/lyrics/" grouped by that folder, folders by name, after the
 * rest: those in the mapset folder (a background) or a top folder (the main "sb/"). Within each,
 * images follow `sort`.
 */
function groupByFolder(images: StoryboardImage[], sort: Sort): Folder[] {
  const folders = new Map<string, Folder>();
  for (const image of images) {
    const path = groupOf(image.path);
    const key = path.toLowerCase();
    let folder = folders.get(key);
    if (!folder) {
      folder = { path, images: [], sizeBytes: 0, missingCount: 0 };
      folders.set(key, folder);
    }
    folder.images.push(image);
    folder.sizeBytes += image.fileSizeBytes;
    if (image.missingFileCount > 0) folder.missingCount++;
  }

  const direction = sort.descending ? -1 : 1;
  for (const folder of folders.values()) {
    folder.images.sort(
      (a, b) => direction * compare(a, b, sort.key) || a.path.localeCompare(b.path)
    );
  }

  return [...folders.values()].sort(
    (a, b) =>
      Number(b.path === '') - Number(a.path === '') ||
      a.path.localeCompare(b.path, undefined, { numeric: true, sensitivity: 'base' })
  );
}

/** The image, or a stand-in when its file isn't in the folder or can't be shown. */
function ImagePreview({
  image,
  beatmapFolderPath,
  height,
  width,
}: {
  image: StoryboardImage;
  beatmapFolderPath: string | undefined;
  height: number | string;
  width?: number;
}) {
  const [failed, setFailed] = useState(false);
  const allMissing = image.isAnimation
    ? image.missingFileCount >= image.frameCount
    : image.missingFileCount > 0;

  if (allMissing || failed || !beatmapFolderPath) {
    return (
      <Box
        h={height}
        w={width}
        bg="dark.8"
        style={{
          flexShrink: 0,
          borderRadius: 'var(--mantine-radius-sm)',
          display: 'grid',
          placeItems: 'center',
        }}
      >
        <IconPhotoOff size={width ? 16 : 24} color="var(--mantine-color-dimmed)" aria-hidden />
      </Box>
    );
  }

  return (
    <Image
      src={buildBeatmapResourceUrl(beatmapFolderPath, image.previewPath)}
      alt=""
      h={height}
      w={width}
      fit="contain"
      radius="sm"
      bg="dark.8"
      loading="lazy"
      style={{ flexShrink: 0 }}
      onError={() => setFailed(true)}
    />
  );
}

/** A column label that sorts the table by it; again flips the order. */
function SortHeader({
  label,
  sortKey,
  sort,
  onSort,
  left = false,
}: {
  label: string;
  sortKey: SortKey;
  sort: Sort;
  onSort: (key: SortKey) => void;
  left?: boolean;
}) {
  const active = sort.key === sortKey;
  const Arrow = sort.descending ? IconArrowDown : IconArrowUp;
  return (
    <Table.Th
      className={left ? 'mv-table-left' : undefined}
      aria-sort={active ? (sort.descending ? 'descending' : 'ascending') : 'none'}
    >
      <UnstyledButton
        onClick={() => onSort(sortKey)}
        style={{ font: 'inherit', color: 'inherit', display: 'inline-flex', gap: 4 }}
      >
        {label}
        {active && <Arrow size={12} aria-hidden />}
      </UnstyledButton>
    </Table.Th>
  );
}

/**
 * Every image the storyboard uses, once each, an animation's frames together, in a table with
 * subfolders grouped. Picking one shows it larger with where it's used.
 */
function StoryboardImages({ images, beatmapFolderPath }: StoryboardImagesProps) {
  const [sort, setSort] = useState<Sort>({ key: 'name', descending: false });
  const folders = useMemo(() => groupByFolder(images, sort), [images, sort]);
  const [closed, setClosed] = useState<Set<string>>(
    () =>
      new Set(
        images.length > OPEN_ALL_UP_TO
          ? folders.map((folder) => folder.path).filter((path) => path !== '')
          : []
      )
  );
  const [openImage, setOpenImage] = useState<StoryboardImage | null>(null);

  // The card's own width, not the window's: beside the file list it is far narrower. Columns drop
  // least needed first, so the file name always keeps room.
  const { ref, width } = useElementSize();
  const measured = width > 0;
  // By importance: File, then Uses, Layer, Resolution and Size; the least important goes first.
  const showSize = !measured || width >= 600;
  const showResolution = !measured || width >= 500;
  const showLayer = !measured || width >= 400;
  const showUses = !measured || width >= 320;
  const columnCount =
    1 + Number(showUses) + Number(showLayer) + Number(showResolution) + Number(showSize);

  const onSort = (key: SortKey) =>
    setSort((current) =>
      current.key === key
        ? { key, descending: !current.descending }
        : { key, descending: key !== 'name' }
    );

  const toggleFolder = (path: string) =>
    setClosed((current) => {
      const next = new Set(current);
      if (next.has(path)) next.delete(path);
      else next.add(path);
      return next;
    });

  return (
    <SectionCard
      title="Images"
      info="Each image the storyboard uses, with those in subfolders such as sb/lyrics grouped by folder, folders by name. Within each, the rows follow the column you sort by. An animation's frames are one row, shown by its first frame. Click a row to see it larger."
    >
      <Box ref={ref}>
        <AppTable verticalSpacing="xs">
          <Table.Thead>
            <Table.Tr>
              <SortHeader label="File" sortKey="name" sort={sort} onSort={onSort} left />
              {showUses && <SortHeader label="Uses" sortKey="uses" sort={sort} onSort={onSort} />}
              {showLayer && <Table.Th>Layer</Table.Th>}
              {showResolution && (
                <SortHeader label="Resolution" sortKey="resolution" sort={sort} onSort={onSort} />
              )}
              {showSize && <SortHeader label="Size" sortKey="size" sort={sort} onSort={onSort} />}
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {folders.map((folder) => {
              const isGroup = folder.path !== '';
              const isClosed = isGroup && closed.has(folder.path);
              const Chevron = isClosed ? IconChevronRight : IconChevronDown;
              return [
                isGroup && (
                  <Table.Tr key={`folder:${folder.path}`}>
                    <Table.Td
                      colSpan={columnCount}
                      className="mv-table-left"
                      style={{ maxWidth: 0 }}
                    >
                      <UnstyledButton
                        onClick={() => toggleFolder(folder.path)}
                        aria-expanded={!isClosed}
                        style={{ width: '100%' }}
                      >
                        <Group gap="xs" wrap="nowrap">
                          <Chevron size={14} aria-hidden />
                          <Text
                            size="sm"
                            fw={600}
                            truncate
                            style={{ flexShrink: 0, maxWidth: '60%' }}
                          >
                            {folder.path}
                          </Text>
                          <Text size="sm" c="dimmed" truncate>
                            {countWord(folder.images.length, 'image')} ·{' '}
                            {formatBytes(folder.sizeBytes)}
                            {folder.missingCount > 0 && (
                              <Text span inherit c="orange">
                                {' '}
                                · {folder.missingCount} missing
                              </Text>
                            )}
                          </Text>
                        </Group>
                      </UnstyledButton>
                    </Table.Td>
                  </Table.Tr>
                ),
                ...(isClosed
                  ? []
                  : folder.images.map((image) => {
                      const allMissing = image.missingFileCount >= image.frameCount;
                      return (
                        <Table.Tr
                          key={`${image.isAnimation ? 'a' : 's'}:${image.path}`}
                          onClick={() => setOpenImage(image)}
                          style={{ cursor: 'pointer' }}
                        >
                          <Table.Td
                            className="mv-table-left"
                            style={{ maxWidth: 0, width: '100%' }}
                          >
                            <Group
                              gap="sm"
                              wrap="nowrap"
                              pl={isGroup ? 'lg' : 0}
                              style={{ minWidth: 0 }}
                            >
                              <ImagePreview
                                image={image}
                                beatmapFolderPath={beatmapFolderPath}
                                height={THUMBNAIL_SIZE}
                                width={THUMBNAIL_SIZE}
                              />
                              <Stack gap={0} style={{ minWidth: 0 }}>
                                <UnstyledButton
                                  onClick={(event) => {
                                    event.stopPropagation();
                                    setOpenImage(image);
                                  }}
                                  style={{ minWidth: 0 }}
                                >
                                  <Text size="sm" truncate title={image.path}>
                                    {image.path.slice(folder.path.length)}
                                  </Text>
                                </UnstyledButton>
                                {/* Missing files show here, not under Size, so they stay in
                                    view when the less important columns are hidden. */}
                                {image.isAnimation ? (
                                  <Text size="xs" c="dimmed" truncate title={missingTitle(image)}>
                                    {countWord(image.frameCount, 'frame')}
                                    {image.missingFiles.length > 0 && (
                                      <Text span inherit c="orange">
                                        {allMissing
                                          ? ' · all missing'
                                          : ` · missing ${image.missingFiles.map(nameOf).join(', ')}`}
                                      </Text>
                                    )}
                                  </Text>
                                ) : (
                                  allMissing && (
                                    <Text size="xs" c="orange">
                                      Missing
                                    </Text>
                                  )
                                )}
                              </Stack>
                            </Group>
                          </Table.Td>
                          {showUses && <Table.Td>{image.useCount.toLocaleString()}</Table.Td>}
                          {showLayer && (
                            <Table.Td title={image.layers.join(', ')}>
                              {image.layers[0] ?? '–'}
                              {image.layers.length > 1 && ` +${image.layers.length - 1}`}
                            </Table.Td>
                          )}
                          {showResolution && <Table.Td>{image.resolution || '–'}</Table.Td>}
                          {showSize && (
                            <Table.Td>{allMissing ? '–' : image.fileSizeFormatted}</Table.Td>
                          )}
                        </Table.Tr>
                      );
                    })),
              ];
            })}
          </Table.Tbody>
        </AppTable>
      </Box>

      <Modal
        opened={!!openImage}
        onClose={() => setOpenImage(null)}
        title={openImage?.path}
        size="xl"
        centered
      >
        {openImage &&
          (openImage.isAnimation && openImage.missingFileCount >= openImage.frameCount ? (
            <Stack gap="md">
              <EmptyState
                icon={IconPhotoOff}
                title="Frames missing"
                description="None of this animation's frames are in the mapset folder."
              />
              <MissingFiles image={openImage} />
            </Stack>
          ) : (
            <Stack gap="md">
              <ImagePreview image={openImage} beatmapFolderPath={beatmapFolderPath} height="60vh" />
              <SimpleGrid cols={{ base: 2, sm: 3 }} spacing="md">
                <StatField label="Resolution" value={openImage.resolution || 'Unknown'} />
                <StatField
                  label="Size"
                  value={openImage.fileSizeFormatted}
                  note={openImage.isAnimation ? 'All frames' : undefined}
                />
                <StatField
                  label="Uses"
                  value={openImage.useCount.toLocaleString()}
                  note={
                    openImage.isAnimation ? countWord(openImage.frameCount, 'frame') : undefined
                  }
                />
                <StatField label="Layers" value={openImage.layers.join(', ') || 'Unknown'} />
                <Box style={{ gridColumn: 'span 2' }}>
                  <StatField label="From" value={openImage.sources.join(', ')} />
                </Box>
              </SimpleGrid>
              <MissingFiles image={openImage} />
            </Stack>
          ))}
      </Modal>
    </SectionCard>
  );
}

export default StoryboardImages;
