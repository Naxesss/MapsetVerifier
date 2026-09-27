import { Text, Badge, Group, useMantineTheme, Stack, Tooltip, Box } from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import { DifficultyColourSettings, ComboColourInfo, ColourInfo } from '../../../Types';
import { formatGameModeLabel } from '../../../utils/gameMode';
import SectionCard from '../../common/SectionCard.tsx';

interface ColourSettingsProps {
  colourSettings: DifficultyColourSettings[];
}

function ColourSwatch({
  colour,
  label,
  showWarning = true,
}: {
  colour: ComboColourInfo | ColourInfo;
  label?: string;
  showWarning?: boolean;
}) {
  const theme = useMantineTheme();
  const hasWarning = showWarning && colour.luminosityWarning;

  return (
    <Tooltip
      label={
        <Stack gap="2xs">
          <Text size="xs">
            RGB: {colour.r}, {colour.g}, {colour.b}
          </Text>
          <Text size="xs">HSP Luminosity: {colour.hspLuminosity.toFixed(1)}</Text>
          {hasWarning && (
            <Text size="xs" c="yellow">
              {colour.luminosityWarning}
            </Text>
          )}
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
            position: 'relative',
          }}
        >
          {hasWarning && (
            <IconAlertTriangle
              size={10}
              style={{
                position: 'absolute',
                top: -4,
                right: -4,
                color: theme.colors.yellow[5],
              }}
            />
          )}
        </Box>
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
              <ColourSwatch colour={settings.sliderTrack} showWarning={false} />
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

function ColourGroupDisplay({ group, isOnlyGroup }: { group: ColourGroup; isOnlyGroup: boolean }) {
  const theme = useMantineTheme();
  const hasWarning =
    group.settings.comboColours.some((c) => c.luminosityWarning) ||
    group.settings.sliderBorder?.luminosityWarning;

  return (
    <Box
      p="sm"
      style={{
        backgroundColor: theme.colors.dark[6],
        borderRadius: theme.radius.sm,
      }}
    >
      {/* Which difficulties use these colours; one group means they all share them. */}
      <Group gap="xs" mb="xs" wrap="wrap">
        <Text size="xs" c="dimmed">
          {isOnlyGroup ? 'Used by all difficulties' : 'Used by'}
        </Text>
        {!isOnlyGroup && group.difficulties.map((diff, idx) => <Badge key={idx}>{diff}</Badge>)}
        {hasWarning && <IconAlertTriangle size={12} style={{ color: theme.colors.yellow[5] }} />}
      </Group>
      <DifficultyColours settings={group.settings} />
    </Box>
  );
}

function ColourSettings({ colourSettings }: ColourSettingsProps) {
  if (colourSettings.length === 0) {
    return null;
  }

  const groups = groupByColours(colourSettings);
  const hasWarnings = colourSettings.some(
    (s) => s.comboColours.some((c) => c.luminosityWarning) || s.sliderBorder?.luminosityWarning
  );

  return (
    <SectionCard
      title="Colours"
      info="Combo colours and slider colours. HSP luminosity < 43 is too dark, > 250 is too bright for kiai."
      actions={
        hasWarnings && (
          <Badge color="yellow" leftSection={<IconAlertTriangle size={12} />}>
            Luminosity warnings
          </Badge>
        )
      }
    >
      <Stack gap="sm">
        {groups.map((group) => (
          <ColourGroupDisplay key={group.key} group={group} isOnlyGroup={groups.length === 1} />
        ))}
      </Stack>
    </SectionCard>
  );
}

export default ColourSettings;
