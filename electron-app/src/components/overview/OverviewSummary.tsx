import { Box, Group, Skeleton, Stack, Text, Tooltip, UnstyledButton } from '@mantine/core';
import { IconChevronRight } from '@tabler/icons-react';
import { Fragment } from 'react';
import { useAudioAnalysis } from './audio/hooks/useAudioAnalysis.ts';
import { useBeatmapAnalysis } from './beatmap/hooks/useBeatmapAnalysis.ts';
import { useDifficultyOverview } from './difficulty/hooks/useDifficultyOverview.ts';
import { useMetadataAnalysis } from './metadata/hooks/useMetadataAnalysis.ts';
import { useObjectsAnalysis } from './objects/hooks/useObjectsAnalysis.ts';
import { formatPreciseTime } from './objects/timelineUtils.ts';
import { SECTION_ICONS } from './overviewSections.ts';
import { useVideoAnalysis } from './video/hooks/useVideoAnalysis.ts';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';
import { countWord, pluralize } from '../../utils/countWord.ts';
import { getDifficultyColor } from '../common/DifficultyColor.ts';
import { CardTitle } from '../common/Headings.tsx';
import SectionCard from '../common/SectionCard.tsx';
import StarRatingBadge from '../common/StarRatingBadge.tsx';
import type { OverviewTab } from '../navbar/pageHints.tsx';
import type { ReactNode } from 'react';

interface OverviewSummaryProps {
  onOpen: (section: OverviewTab) => void;
}

/**
 * The Overview's first page: a row per page with its key facts on one line, each opening that
 * page. The rows always cover every difficulty; picking a mode and difficulties is done on the
 * pages.
 */
export default function OverviewSummary({ onOpen }: OverviewSummaryProps) {
  return (
    <Stack gap="xs" p="md">
      <GeneralRow onOpen={onOpen} />
      <ObjectsRow onOpen={onOpen} />
      <BeatmapRow onOpen={onOpen} />
      <DifficultyRow onOpen={onOpen} />
      <AudioRow onOpen={onOpen} />
      <VideoRow onOpen={onOpen} />
    </Stack>
  );
}

function useAnalysisArgs() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  return { folder, songFolder: settings.songFolder };
}

interface QueryState<T> {
  data: T | undefined;
  isLoading: boolean;
  isError: boolean;
}

interface SummaryRowProps<T extends { success: boolean }> {
  section: OverviewTab;
  onOpen: (section: OverviewTab) => void;
  query: QueryState<T>;
  /** Nothing to open: the row stays, greyed out. */
  disabled?: boolean;
  /** The facts, on one line; cut off with "…" rather than wrapping. */
  children: (data: T) => ReactNode;
}

/** A row that opens its page; shows a skeleton while the analysis runs and says when it failed. */
function SummaryRow<T extends { success: boolean }>({
  section,
  onOpen,
  query,
  disabled,
  children,
}: SummaryRowProps<T>) {
  const Icon = SECTION_ICONS[section];
  const { data, isLoading, isError } = query;
  const failed = isError || (data && !data.success);

  return (
    <UnstyledButton
      className="mv-summary-card"
      aria-label={`Open ${section}`}
      disabled={disabled}
      onClick={() => onOpen(section)}
    >
      <SectionCard padding="sm">
        <Group gap="md" wrap="nowrap">
          <Group gap="xs" wrap="nowrap" w={112} style={{ flexShrink: 0 }}>
            <Icon size={18} stroke={1.5} aria-hidden />
            <CardTitle>{section}</CardTitle>
          </Group>
          <Box style={{ flex: 1, minWidth: 0 }}>
            {isLoading && <Skeleton height={16} radius="md" maw={240} />}
            {failed && (
              <Text size="sm" c="dimmed" truncate>
                Couldn&apos;t analyze this. Open the page for details.
              </Text>
            )}
            {data?.success && children(data)}
          </Box>
          <IconChevronRight
            size={16}
            stroke={1.5}
            color="var(--mantine-color-dimmed)"
            style={{ flexShrink: 0 }}
            aria-hidden
          />
        </Group>
      </SectionCard>
    </UnstyledButton>
  );
}

interface Fact {
  /** What stands out, in bold. */
  value: ReactNode;
  /** What it is, dimmed after the value. */
  label?: string;
}

/** Facts on one line, separated by dots and cut off with "…" when the row runs out of room. */
function Facts({ facts }: { facts: Fact[] }) {
  return (
    <Text size="sm" truncate>
      {facts.map((fact, i) => (
        <Fragment key={i}>
          {i > 0 && (
            <Text span c="dimmed" mx={6} aria-hidden>
              ·
            </Text>
          )}
          <Text span fw={600} inherit>
            {fact.value}
          </Text>
          {fact.label && (
            <Text span c="dimmed" inherit>
              {' '}
              {fact.label}
            </Text>
          )}
        </Fragment>
      ))}
    </Text>
  );
}

const NONE_TO_SHOW = <Facts facts={[{ value: 'Nothing to show' }]} />;

function GeneralRow({ onOpen }: OverviewSummaryProps) {
  const query = useMetadataAnalysis(useAnalysisArgs());

  return (
    <SummaryRow section="General" onOpen={onOpen} query={query}>
      {(data) => (
        <Facts
          facts={[
            { value: data.resources.totalFolderSizeFormatted, label: 'folder' },
            {
              value: data.resources.hitSounds.length,
              label: pluralize(data.resources.hitSounds.length, 'hit sound'),
            },
            {
              value: data.resources.backgrounds.length,
              label: pluralize(data.resources.backgrounds.length, 'background'),
            },
          ]}
        />
      )}
    </SummaryRow>
  );
}

function ObjectsRow({ onOpen }: OverviewSummaryProps) {
  const query = useObjectsAnalysis(useAnalysisArgs());

  return (
    <SummaryRow section="Objects" onOpen={onOpen} query={query}>
      {(data) => {
        const objects = data.difficulties.reduce((total, d) => total + d.objectCount, 0);
        return (
          <Facts
            facts={[
              { value: objects.toLocaleString(), label: pluralize(objects, 'hit object') },
              { value: countWord(data.difficulties.length, 'difficulty') },
            ]}
          />
        );
      }}
    </SummaryRow>
  );
}

/** The lowest and highest of the values that parse as numbers, e.g. "8 – 9.3"; null if none do. */
function rangeOf(values: (string | number | null)[]) {
  const numbers = values.map((v) => (v == null ? NaN : Number(v))).filter((n) => !Number.isNaN(n));
  if (numbers.length === 0) return null;
  const format = (n: number) => String(Math.round(n * 10) / 10);
  const low = Math.min(...numbers);
  const high = Math.max(...numbers);
  return low === high ? format(low) : `${format(low)} – ${format(high)}`;
}

function BeatmapRow({ onOpen }: OverviewSummaryProps) {
  const query = useBeatmapAnalysis(useAnalysisArgs());

  return (
    <SummaryRow section="Beatmap" onOpen={onOpen} query={query}>
      {(data) => {
        const settings = data.difficultySettings;
        const ranges = [
          { label: 'CS', range: rangeOf(settings.map((s) => s.circleSize)) },
          { label: 'AR', range: rangeOf(settings.map((s) => s.approachRate)) },
          { label: 'OD', range: rangeOf(settings.map((s) => s.overallDifficulty)) },
          { label: 'HP', range: rangeOf(settings.map((s) => s.hpDrain)) },
        ].flatMap(({ label, range }) => (range ? [{ value: range, label }] : []));

        return ranges.length > 0 ? <Facts facts={ranges} /> : NONE_TO_SHOW;
      }}
    </SummaryRow>
  );
}

function DifficultyRow({ onOpen }: OverviewSummaryProps) {
  const query = useDifficultyOverview(useAnalysisArgs());

  return (
    <SummaryRow section="Difficulty" onOpen={onOpen} query={query}>
      {(data) => {
        if (data.difficulties.length === 0) return NONE_TO_SHOW;
        const sorted = [...data.difficulties].sort((a, b) => a.starRating - b.starRating);
        const lowest = sorted[0];
        const highest = sorted[sorted.length - 1];
        const pair = [lowest.starRating, highest.starRating];

        return (
          <Group gap="sm" wrap="nowrap">
            <StarRatingBadge rating={lowest.starRating} size="sm" sizeTo={pair} />
            {sorted.length > 1 && (
              <>
                <Group gap={3} wrap="nowrap" aria-label="Difficulties by star rating">
                  {sorted.map((d) => (
                    <Tooltip key={`${d.mode}-${d.version}`} label={d.label}>
                      <Box
                        w={8}
                        h={8}
                        style={{
                          borderRadius: 2,
                          backgroundColor: getDifficultyColor(d.starRating),
                        }}
                      />
                    </Tooltip>
                  ))}
                </Group>
                <StarRatingBadge rating={highest.starRating} size="sm" sizeTo={pair} />
              </>
            )}
          </Group>
        );
      }}
    </SummaryRow>
  );
}

function AudioRow({ onOpen }: OverviewSummaryProps) {
  const query = useAudioAnalysis(useAnalysisArgs());

  return (
    <SummaryRow section="Audio" onOpen={onOpen} query={query}>
      {(data) => {
        const facts: Fact[] = [];
        if (data.formatAnalysis) facts.push({ value: data.formatAnalysis.format });
        if (data.bitrateAnalysis) {
          facts.push({ value: `${Math.round(data.bitrateAnalysis.averageBitrate)} kbps` });
        }
        if (data.formatAnalysis) {
          facts.push({ value: formatPreciseTime(data.formatAnalysis.durationMs) });
        }
        return facts.length > 0 ? <Facts facts={facts} /> : NONE_TO_SHOW;
      }}
    </SummaryRow>
  );
}

function VideoRow({ onOpen }: OverviewSummaryProps) {
  const query = useVideoAnalysis(useAnalysisArgs());

  return (
    <SummaryRow
      section="Video"
      onOpen={onOpen}
      query={query}
      disabled={query.data?.success && query.data.videos.length === 0}
    >
      {(data) => {
        const video = data.videos[0];
        if (!video) return <Facts facts={[{ value: 'None' }]} />;

        const facts: Fact[] = [{ value: video.resolution }];
        if (video.frameRate != null) facts.push({ value: `${Math.round(video.frameRate)} fps` });
        facts.push({ value: video.fileSizeFormatted });
        if (data.videos.length > 1) facts.push({ value: data.videos.length, label: 'videos' });
        return <Facts facts={facts} />;
      }}
    </SummaryRow>
  );
}
