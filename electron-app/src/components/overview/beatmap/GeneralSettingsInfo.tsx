import { Badge, Text, Tooltip } from '@mantine/core';
import { countWord } from '../../../utils/countWord.ts';
import OsuLink from '../../common/OsuLink.tsx';
import ComparisonTable, { type ComparisonRow } from '../ComparisonTable.tsx';
import type { DifficultyGeneralSettings } from '../../../Types';

interface GeneralSettingsInfoProps {
  generalSettings: DifficultyGeneralSettings[];
  /** Star rating per difficulty version, for the column headers. */
  starRatings: Map<string, number>;
}

const text = (value: string) => <Text size="sm">{value}</Text>;

function YesNoBadge({ value }: { value: boolean }) {
  return (
    <Badge color={value ? 'green' : 'gray'} variant={value ? 'light' : 'outline'}>
      {value ? 'Yes' : 'No'}
    </Badge>
  );
}

function CountdownCell({ settings }: { settings: DifficultyGeneralSettings }) {
  if (!settings.countdownSpeed) {
    return <Badge color="gray">No</Badge>;
  }

  // Enabled but with no room before the first object, osu! never plays it.
  if (settings.countdownInsufficientTime) {
    return (
      <Tooltip
        multiline
        w={260}
        label="Enabled, but there isn't enough time before the first object for it to play."
      >
        <Badge color="gray">{settings.countdownSpeed}, not played</Badge>
      </Tooltip>
    );
  }

  return <Badge>{settings.countdownSpeed}</Badge>;
}

// A null value means the setting doesn't apply (e.g. stack leniency outside osu!), so the row
// is left out.
const ROWS: ComparisonRow<DifficultyGeneralSettings>[] = [
  {
    id: 'audioFileName',
    label: 'Audio file',
    value: (s) => s.audioFileName,
    render: (s) => text(s.audioFileName),
  },
  {
    id: 'audioLeadIn',
    label: 'Lead-in',
    value: (s) => s.audioLeadIn,
    render: (s) => text(`${s.audioLeadIn.toLocaleString()} ms`),
  },
  {
    id: 'previewTime',
    label: 'Preview',
    groupColours: true,
    value: (s) => s.previewTime,
    render: (s) => (
      <Text size="sm">
        <OsuLink text={s.previewTimeFormatted} disableSeparators />
      </Text>
    ),
  },
  {
    id: 'stackLeniency',
    label: 'Stack leniency',
    value: (s) => s.stackLeniency,
    render: (s) => text(s.stackLeniency ?? 'N/A'),
  },
  {
    id: 'countdown',
    label: 'Countdown',
    value: (s) => `${s.countdownSpeed ?? ''}|${s.countdownInsufficientTime}`,
    render: (s) => <CountdownCell settings={s} />,
  },
  {
    id: 'countdownOffset',
    label: 'Countdown offset',
    value: (s) => (s.countdownSpeed !== null ? s.countdownOffset : null),
    render: (s) =>
      text(
        s.countdownSpeed !== null && s.countdownOffset !== null
          ? countWord(s.countdownOffset, 'beat')
          : 'N/A'
      ),
  },
  {
    id: 'letterboxInBreaks',
    label: 'Letterbox in breaks',
    value: (s) => s.letterboxInBreaks,
    render: (s) => <YesNoBadge value={s.letterboxInBreaks} />,
  },
  {
    id: 'widescreenStoryboard',
    label: 'Widescreen storyboard',
    value: (s) => s.widescreenStoryboard,
    render: (s) => <YesNoBadge value={s.widescreenStoryboard} />,
  },
  {
    id: 'useSkinSprites',
    label: 'Use skin sprites',
    value: (s) => s.useSkinSprites,
    render: (s) => text(s.useSkinSprites ?? 'N/A'),
  },
  {
    id: 'skinPreference',
    label: 'Skin preference',
    value: (s) => s.skinPreference,
    render: (s) => text(s.skinPreference || 'None'),
  },
  {
    id: 'epilepsyWarning',
    label: 'Epilepsy warning',
    value: (s) => s.epilepsyWarning,
    render: (s) => text(s.epilepsyWarning ?? 'N/A'),
  },
];

function GeneralSettingsInfo({ generalSettings, starRatings }: GeneralSettingsInfoProps) {
  return (
    <ComparisonTable
      starRatings={starRatings}
      title="General settings"
      items={generalSettings}
      rows={ROWS}
    />
  );
}

export default GeneralSettingsInfo;
