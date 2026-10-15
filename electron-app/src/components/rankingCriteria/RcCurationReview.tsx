import {
  Alert,
  Badge,
  Button,
  Group,
  Kbd,
  Paper,
  Progress,
  SegmentedControl,
  Select,
  Skeleton,
  Stack,
  Text,
  Textarea,
} from '@mantine/core';
import { getHotkeyHandler, useHotkeys } from '@mantine/hooks';
import { IconAlertCircle, IconArrowLeft, IconArrowRight } from '@tabler/icons-react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import RcStatementDetails from './RcStatementDetails';
import { rankingCriteriaRoute } from './rcUtils';
import { useAllRankingCriteriaPages, useRankingCriteriaOverview } from './useRankingCriteria';
import { apiFetch } from '../../client/ApiHelper';
import { ApiRcStatement, RcAutomation } from '../../Types';

const ALL = 'all';

/**
 * Writes a statement's curation to its catalogue file. Kept here rather than in RankingCriteriaApi
 * so it is only bundled with this dev-only screen; the endpoint only exists on a Debug backend.
 */
async function updateCuration(curation: {
  id: string;
  automation: RcAutomation;
  notes: string | null;
}) {
  const res = await apiFetch('/ranking-criteria/curation', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(curation),
  });
  if (!res.ok) {
    const message = await res.text();
    // A Release backend has no such route, so it answers without a message of its own.
    throw new Error(
      message ||
        (res.status === 404 || res.status === 405
          ? 'The backend has no curation endpoint. Run a Debug build of the backend.'
          : `Saving failed (${res.status})`)
    );
  }

  return (await res.json()) as ApiRcStatement;
}

const AUTOMATIONS: { value: RcAutomation; label: string }[] = [
  { value: 'Unknown', label: 'Unknown' },
  { value: 'Automatable', label: 'Automatable' },
  { value: 'Partial', label: 'Partial' },
  { value: 'Manual', label: 'Manual' },
];

function sectionOf(statement: ApiRcStatement) {
  return statement.path.join(' › ') || statement.pageTitle;
}

/** Intros are finished by their nested statements and allowances have nothing to enforce. */
function isReviewable(statement: ApiRcStatement) {
  return !statement.intro && statement.kind !== 'Allowance';
}

interface CurationFormProps {
  statement: ApiRcStatement;
  isSaving: boolean;
  onSave: (automation: RcAutomation, notes: string) => void;
  onPrevious: (() => void) | null;
  onNext: (notes: string) => void;
}

/** The decision for one statement. Keyed by statement, so the notes draft starts over for each. */
function CurationForm({ statement, isSaving, onSave, onPrevious, onNext }: CurationFormProps) {
  const [notes, setNotes] = useState(statement.notes ?? '');

  useHotkeys([
    ...AUTOMATIONS.map(
      ({ value }, index) => [String(index + 1), () => onSave(value, notes)] as [string, () => void]
    ),
    ['ArrowLeft', () => onPrevious?.()],
    ['ArrowRight', () => onNext(notes)],
  ]);

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="xs">
        <SegmentedControl
          fullWidth
          value={statement.automation}
          disabled={isSaving}
          onChange={(value) => onSave(value as RcAutomation, notes)}
          data={AUTOMATIONS.map(({ value, label }, index) => ({
            value,
            label: (
              <Group gap={6} justify="center" wrap="nowrap">
                <Kbd size="xs">{index + 1}</Kbd>
                {label}
              </Group>
            ),
          }))}
        />
        <Textarea
          placeholder="Notes (optional)"
          autosize
          minRows={1}
          maxRows={4}
          value={notes}
          onChange={(event) => setNotes(event.currentTarget.value)}
          onKeyDown={getHotkeyHandler([['mod+Enter', () => onNext(notes)]])}
        />
        <Group justify="space-between">
          <Button
            variant="default"
            leftSection={<IconArrowLeft size={16} />}
            disabled={!onPrevious || isSaving}
            onClick={() => onPrevious?.()}
          >
            Previous
          </Button>
          <Text size="xs" c="dimmed">
            <Kbd size="xs">1</Kbd>–<Kbd size="xs">4</Kbd> saves and moves on, <Kbd size="xs">←</Kbd>{' '}
            <Kbd size="xs">→</Kbd> browse without changing
          </Text>
          <Button
            variant="default"
            rightSection={<IconArrowRight size={16} />}
            disabled={isSaving}
            onClick={() => onNext(notes)}
          >
            Next
          </Button>
        </Group>
      </Stack>
    </Paper>
  );
}

/**
 * Dev-only: steps through the ranking criteria one statement at a time and writes each decision to
 * its catalogue file in MapsetVerifier.RankingCriteria/Data, ready to commit.
 */
export default function RcCurationReview() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();

  const pageKey = searchParams.get('page') ?? ALL;
  const section = searchParams.get('section') ?? ALL;
  const [curation, setCuration] = useState<RcAutomation | typeof ALL>('Unknown');
  const [index, setIndex] = useState(0);
  // Saved this session, so they stay in the queue after their curation changes and Previous can
  // return to them.
  const [saved, setSaved] = useState<Set<string>>(new Set());

  const overview = useRankingCriteriaOverview();
  const { statements, isLoading, isError } = useAllRankingCriteriaPages(overview.data);

  const save = useMutation({
    mutationFn: updateCuration,
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ['rankingCriteriaPage'] }),
        queryClient.invalidateQueries({ queryKey: ['rankingCriteriaOverview'] }),
      ]),
  });

  const reviewable = statements.filter(isReviewable);
  const onPage = reviewable.filter((statement) => pageKey === ALL || statement.page === pageKey);
  const inScope = onPage.filter((statement) => section === ALL || sectionOf(statement) === section);
  const queue = inScope.filter(
    (statement) => curation === ALL || statement.automation === curation || saved.has(statement.id)
  );
  const position = Math.min(index, queue.length);
  const current = queue[position] as ApiRcStatement | undefined;

  const pages = (overview.data?.pages ?? []).filter((page) => page.hasStatements);
  const sections = [...new Set(onPage.map(sectionOf))];

  const setScope = (page: string, nextSection: string) => {
    const params = new URLSearchParams();
    if (page !== ALL) params.set('page', page);
    if (nextSection !== ALL) params.set('section', nextSection);
    setSearchParams(params, { replace: true });
    setIndex(0);
  };

  const commit = (automation: RcAutomation, notes: string) => {
    if (!current) return;

    const trimmed = notes.trim() || null;
    if (automation === current.automation && trimmed === (current.notes ?? null)) {
      setIndex(position + 1);
      return;
    }

    setSaved((previous) => new Set(previous).add(current.id));
    save.mutate(
      { id: current.id, automation, notes: trimmed },
      { onSuccess: () => setIndex(position + 1) }
    );
  };

  const unknownLeft = reviewable.filter((statement) => statement.automation === 'Unknown').length;

  return (
    <Stack gap="sm">
      <Stack gap="xs">
        <Group justify="space-between" align="baseline">
          <Group gap="xs" align="baseline">
            <Text fw={700} size="md">
              Curation review
            </Text>
            <Badge size="sm" variant="light" color="orange">
              Dev only
            </Badge>
          </Group>
          <Text size="xs" c="dimmed">
            {unknownLeft} of {reviewable.length} rules and guidelines still unknown
          </Text>
        </Group>
        <Text size="xs" c="dimmed">
          Each decision is written to
          MapsetVerifier.RankingCriteria/Data/catalogue/&lt;page&gt;.json right away, ready to
          commit.
        </Text>
      </Stack>

      <Group gap="sm">
        <Button
          variant="default"
          leftSection={<IconArrowLeft size={16} />}
          onClick={() => navigate(rankingCriteriaRoute(pageKey === ALL ? 'general' : pageKey))}
        >
          Back
        </Button>
        <Select
          aria-label="Page"
          w={200}
          allowDeselect={false}
          value={pageKey}
          data={[
            { value: ALL, label: 'All pages' },
            ...pages.map((page) => ({ value: page.key, label: page.title })),
          ]}
          onChange={(value) => value && setScope(value, ALL)}
        />
        <Select
          aria-label="Section"
          style={{ flex: 1, minWidth: 220 }}
          allowDeselect={false}
          disabled={pageKey === ALL}
          value={section}
          data={[{ value: ALL, label: 'All sections' }, ...sections]}
          onChange={(value) => value && setScope(pageKey, value)}
        />
        <Select
          aria-label="Curation"
          w={180}
          allowDeselect={false}
          value={curation}
          data={[
            { value: ALL, label: `Any curation (${inScope.length})` },
            ...AUTOMATIONS.map(({ value, label }) => ({
              value,
              label: `${label} (${inScope.filter((statement) => statement.automation === value).length})`,
            })),
          ]}
          onChange={(value) => {
            if (!value) return;
            setCuration(value as RcAutomation | typeof ALL);
            // Starts a fresh pass, so rules saved under the previous curation don't linger.
            setSaved(new Set());
            setIndex(0);
          }}
        />
      </Group>

      <Group gap="sm" wrap="nowrap">
        <Progress
          style={{ flex: 1 }}
          size="md"
          radius="xl"
          value={queue.length > 0 ? (position / queue.length) * 100 : 0}
        />
        <Text size="xs" c="dimmed" style={{ whiteSpace: 'nowrap' }}>
          {Math.min(position + 1, queue.length)} of {queue.length}
        </Text>
      </Group>

      {(overview.error || isError) && (
        <Alert icon={<IconAlertCircle />} color="red">
          Failed to load the ranking criteria.
        </Alert>
      )}
      {save.error && (
        <Alert icon={<IconAlertCircle />} color="red" withCloseButton onClose={save.reset}>
          {save.error.message}
        </Alert>
      )}

      {isLoading ? (
        <Skeleton height={320} radius="md" />
      ) : current ? (
        <>
          <CurationForm
            key={current.id}
            statement={current}
            isSaving={save.isPending}
            onSave={commit}
            onPrevious={position > 0 ? () => setIndex(position - 1) : null}
            onNext={(notes) => commit(current.automation, notes)}
          />
          <Paper p="lg" radius="md" withBorder>
            <RcStatementDetails statement={current} withoutTitle />
          </Paper>
        </>
      ) : (
        <Stack align="center" gap="xs" py="xl">
          <Text size="sm" c="dimmed">
            {queue.length === 0
              ? 'Nothing to review here.'
              : 'Reviewed everything here. Commit the catalogue changes when ready.'}
          </Text>
          {position > 0 && (
            <Button variant="subtle" size="compact-sm" onClick={() => setIndex(0)}>
              Start over
            </Button>
          )}
        </Stack>
      )}
    </Stack>
  );
}
