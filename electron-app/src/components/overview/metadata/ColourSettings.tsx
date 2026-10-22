import { Badge, Box, Divider, Group, Stack, Text, Tooltip, useMantineTheme } from '@mantine/core';
import { DifficultyColourSettings, ComboColourInfo, ColourInfo } from '../../../Types';
import { formatGameModeLabel } from '../../../utils/gameMode';
import SectionCard from '../../common/SectionCard.tsx';

interface ColourSettingsProps {
  colourSettings: DifficultyColourSettings[];
}

function ColourSwatch({ colour, label }: { colour: ComboColourInfo | ColourInfo; label?: string }) {
  const theme = useMantineTheme();

  return (
    <Tooltip
      label={
        <Stack gap="2xs">
          <Text size="xs">
            RGB: {colour.r}, {colour.g}, {colour.b}
          </Text>
          <Text size="xs">HSP Luminosity: {colour.hspLuminosity.toFixed(1)}</Text>
        </Stack>
      }
      multiline
      w={200}
    >
      <Box style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
        <Box
          style={{
            width: 24,
            height: 24,
            borderRadius: 4,
            backgroundColor: colour.hex,
            border: `1px solid ${theme.colors.dark[3]}`,
          }}
        />
        {label && <Text size="xs">{label}</Text>}
      </Box>
    </Tooltip>
  );
}

function DifficultyColours({ settings }: { settings: DifficultyColourSettings }) {
  if (!settings.isApplicable) {
    return (
      <Text size="xs" c="dimmed">
        N/A for {formatGameModeLabel(settings.mode)}
      </Text>
    );
  }

  if (settings.comboColours.length === 0 && !settings.sliderBorder && !settings.sliderTrack) {
    return (
      <Text size="sm" c="dimmed">
        Using default colours
      </Text>
    );
  }

  return (
    <Stack gap="sm">
      {/* Combo Colours */}
      {settings.comboColours.length > 0 && (
        <Box>
          <Text size="xs" c="dimmed" mb="xs">
            Combo colours
          </Text>
          <Group gap="xs">
            {settings.comboColours.map((colour, idx) => (
              <ColourSwatch key={idx} colour={colour} label={`${colour.index}`} />
            ))}
          </Group>
        </Box>
      )}

      {/* Slider Colours */}
      {(settings.sliderBorder || settings.sliderTrack) && (
        <Group gap="md">
          {settings.sliderBorder && (
            <Box>
              <Text size="xs" c="dimmed" mb="xs">
                Slider border
              </Text>
              <ColourSwatch colour={settings.sliderBorder} />
            </Box>
          )}
          {settings.sliderTrack && (
            <Box>
              <Text size="xs" c="dimmed" mb="xs">
                Slider track
              </Text>
              <ColourSwatch colour={settings.sliderTrack} />
            </Box>
          )}
        </Group>
      )}
    </Stack>
  );
}

interface ColourGroup {
  key: string;
  difficulties: string[];
  settings: DifficultyColourSettings;
}

function getColourKey(settings: DifficultyColourSettings): string {
  if (!settings.isApplicable) return `na-${settings.mode}`;
  if (settings.comboColours.length === 0 && !settings.sliderBorder && !settings.sliderTrack) {
    return 'default';
  }
  const comboKey = settings.comboColours.map((c) => c.hex).join(',');
  const borderKey = settings.sliderBorder?.hex ?? '';
  const trackKey = settings.sliderTrack?.hex ?? '';
  return `${comboKey}|${borderKey}|${trackKey}`;
}

function groupByColours(colourSettings: DifficultyColourSettings[]): ColourGroup[] {
  const groups = new Map<string, ColourGroup>();

  for (const settings of colourSettings) {
    const key = getColourKey(settings);
    const existing = groups.get(key);
    if (existing) {
      existing.difficulties.push(settings.version);
    } else {
      groups.set(key, {
        key,
        difficulties: [settings.version],
        settings,
      });
    }
  }

  return Array.from(groups.values());
}

/** One set of colours and the difficulties using it; one set means they all share it. */
function ColourGroupDisplay({ group, isOnlyGroup }: { group: ColourGroup; isOnlyGroup: boolean }) {
  return (
    <Stack gap="xs">
      <Group gap="xs" wrap="wrap">
        <Text size="xs" c="dimmed">
          {isOnlyGroup ? 'Used by all difficulties' : 'Used by'}
        </Text>
        {!isOnlyGroup && group.difficulties.map((diff, idx) => <Badge key={idx}>{diff}</Badge>)}
      </Group>
      <DifficultyColours settings={group.settings} />
    </Stack>
  );
}

function ColourSettings({ colourSettings }: ColourSettingsProps) {
  if (colourSettings.length === 0) {
    return null;
  }

  const groups = groupByColours(colourSettings);

  return (
    <SectionCard
      title="Colours"
      info="Combo colours and slider colours. Hover a colour for its RGB value and HSP luminosity."
    >
      <Stack gap="md">
        {groups.map((group, index) => (
          <Stack key={group.key} gap="md">
            {index > 0 && <Divider />}
            <ColourGroupDisplay group={group} isOnlyGroup={groups.length === 1} />
          </Stack>
        ))}
      </Stack>
    </SectionCard>
  );
}

export default ColourSettings;
