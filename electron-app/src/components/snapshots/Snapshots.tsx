import { Alert, Badge, Flex, Text, useMantineTheme } from '@mantine/core';
import { IconAlertCircle, IconHistoryOff, IconPointer } from '@tabler/icons-react';
import { useMemo, useState } from 'react';
import CompareBar from './CompareBar';
import DifficultyChanges from './DifficultyChanges';
import GeneralChanges from './GeneralChanges';
import { useSnapshotCompare } from './hooks/useSnapshotCompare';
import { useSnapshotHistory } from './hooks/useSnapshotHistory';
import PinModal from './PinModal';
import { indexOfEntry, resolveBaseId, type RangePreset } from './range';
import SnapshotHistoryList from './SnapshotHistoryList';
import { useBeatmap } from '../../context/BeatmapContext';
import { useSettings } from '../../context/SettingsContext';
import { notifyError } from '../../utils/notify';
import BeatmapHeader from '../common/BeatmapHeader';
import DifficultyPicker, { GENERAL_TAB_ID } from '../common/DifficultyPicker';
import EmptyState from '../common/EmptyState.tsx';
import { HistorySkeleton } from '../common/LoadingSkeletons.tsx';
import SectionCard from '../common/SectionCard.tsx';
import SelectedDifficultyRow from '../common/SelectedDifficultyRow.tsx';
import StackTraceMessage from '../common/StackTraceMessage.tsx';
import StarRatingBadge from '../common/StarRatingBadge.tsx';
import GameModeIcon from '../icons/GameModeIcon';
import SnapshotDifficultyChangesIcon from '../icons/SnapshotDifficultyChangesIcon';
import type { ApiSnapshotHistoryEntry, ApiSnapshotDifficultyComparison } from '../../Types';

/** Width of the history list; enough for the time, its badges and the change counts. */
const HISTORY_WIDTH = 300;

function hasChanges(difficulty: ApiSnapshotDifficultyComparison) {
  return difficulty.status !== 'Unchanged' && difficulty.counts.total > 0;
}

function Snapshots() {
  const theme = useMantineTheme();
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const [selectedId, setSelectedId] = useState<string | undefined>();
  const [preset, setPreset] = useState<RangePreset>('previous');
  const [customBaseId, setCustomBaseId] = useState<string | undefined>();
  const [targetId, setTargetId] = useState<string | undefined>();
  const [pinning, setPinning] = useState<ApiSnapshotHistoryEntry | null>(null);

  const [prevFolder, setPrevFolder] = useState(folder);
  if (folder !== prevFolder) {
    setPrevFolder(folder);
    setSelectedId(undefined);
    setPreset('previous');
    setCustomBaseId(undefined);
    setTargetId(undefined);
  }

  const history = useSnapshotHistory({ folder, songFolder: settings.songFolder });
  const entries = useMemo(() => history.data?.entries ?? [], [history.data]);

  const effectiveTargetId = entries.some((e) => e.id === targetId) ? targetId : entries[0]?.id;
  const baseId = resolveBaseId(entries, effectiveTargetId, preset, customBaseId);

  const compare = useSnapshotCompare({
    setKey: history.data?.setKey,
    baseId,
    targetId: effectiveTargetId,
  });
  const comparison = compare.data;

  const pickerDifficulties = useMemo(() => {
    if (comparison) {
      return comparison.difficulties.map((d) => ({
        id: d.key,
        label: d.name,
        mode: d.mode,
        starRating:
          d.starsAfter ??
          d.starsBefore ??
          history.data?.difficulties.find((h) => h.key === d.key)?.starRating,
        changed: hasChanges(d),
      }));
    }

    return (history.data?.difficulties ?? []).map((d) => ({
      id: d.key,
      label: d.name,
      mode: d.mode,
      starRating: d.starRating,
      changed: false,
    }));
  }, [comparison, history.data]);

  const validIds = new Set([GENERAL_TAB_ID, ...pickerDifficulties.map((d) => d.id)]);
  const firstChanged = comparison?.difficulties.find(hasChanges)?.key;
  const activeId =
    selectedId && validIds.has(selectedId) ? selectedId : (firstChanged ?? GENERAL_TAB_ID);
  const selectedDifficulty = comparison?.difficulties.find((d) => d.key === activeId);

  // The same colours as the changed and unchanged icons, for the picker's segments.
  const statusColor = (changed: boolean) => (changed ? theme.colors.blue[6] : theme.colors.dark[2]);
  const generalChanged =
    !!comparison &&
    (comparison.general.rollups.length > 0 ||
      comparison.general.settings.length > 0 ||
      comparison.general.files.length > 0);

  const selectEntry = (id: string, asBase: boolean) => {
    const index = indexOfEntry(entries, id);
    const targetIndex = indexOfEntry(entries, effectiveTargetId);

    if (asBase && index > targetIndex) {
      setPreset('custom');
      setCustomBaseId(id);
      return;
    }

    setTargetId(index === 0 ? undefined : id);

    // Moving the end of "all history" makes it a range of its own, from the same start.
    if (preset === 'all' && baseId && indexOfEntry(entries, baseId) > index) {
      setPreset('custom');
      setCustomBaseId(baseId);
      return;
    }

    // A hand-picked start stays only while it is still older than the new end.
    if (preset === 'custom' && customBaseId && indexOfEntry(entries, customBaseId) <= index) {
      setPreset('previous');
    }
  };

  const savePin = (entry: ApiSnapshotHistoryEntry, name: string | null) =>
    history.pin.mutate(
      { id: entry.id, pin: name },
      { onError: (error) => notifyError(error.message || "Couldn't pin the snapshot.") }
    );

  const showContent = !!history.data && entries.length > 0;
  const hasRange = !!baseId && !!effectiveTargetId;

  return (
    <>
      <BeatmapHeader>
        {showContent && pickerDifficulties.length > 0 && (
          <DifficultyPicker
            difficulties={pickerDifficulties.map((d) => ({
              id: d.id,
              label: d.label,
              mode: d.mode,
              starRating: d.starRating,
              icon: <SnapshotDifficultyChangesIcon hasChanges={d.changed} size={18} />,
              statusColor: statusColor(d.changed),
            }))}
            general={{
              icon: <SnapshotDifficultyChangesIcon hasChanges={generalChanged} size={18} />,
              statusColor: statusColor(generalChanged),
            }}
            modeStatus={(_, diffs) => (
              <SnapshotDifficultyChangesIcon
                hasChanges={diffs.some(
                  (d) => pickerDifficulties.find((p) => p.id === d.id)?.changed
                )}
                size={18}
              />
            )}
            selectedId={activeId}
            onSelect={setSelectedId}
          />
        )}
      </BeatmapHeader>

      {history.isLoading && (
        <Flex bg="dark.6" style={{ flex: 1 }}>
          <HistorySkeleton />
        </Flex>
      )}

      {history.isError && (
        <Flex p="md" bg="dark.6">
          <Alert icon={<IconAlertCircle />} color="red" title="Couldn't load the snapshots">
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
              {history.error?.message}
            </Text>
            {history.error?.stackTrace && (
              <StackTraceMessage stackTrace={history.error.stackTrace} />
            )}
          </Alert>
        </Flex>
      )}

      {history.data && entries.length === 0 && (
        <Flex bg="dark.6" p="md" style={{ flex: 1 }}>
          <EmptyState
            icon={IconHistoryOff}
            title="No snapshots yet"
            description="Snapshots are taken when you open this page or run checks. Come back after the mapset changes to see what changed."
            fullHeight
          />
        </Flex>
      )}

      {showContent && (
        <Flex
          gap="md"
          px="md"
          pb="md"
          pt="sm"
          direction="column"
          style={{ flex: 1, minWidth: 0 }}
          bg="dark.6"
        >
          <CompareBar
            entries={entries}
            baseId={baseId}
            targetId={effectiveTargetId}
            preset={preset}
            onPreset={(next) => {
              setPreset(next);
            }}
            onBase={(id) => {
              setPreset('custom');
              setCustomBaseId(id);
            }}
            onTarget={(id) => selectEntry(id, false)}
          />

          <Flex gap="md" align="flex-start" direction={{ base: 'column', md: 'row' }}>
            <SectionCard
              title="History"
              actions={<Badge color="gray">{entries.length}</Badge>}
              w={{ base: '100%', md: HISTORY_WIDTH }}
              style={{ flexShrink: 0 }}
            >
              <SnapshotHistoryList
                entries={entries}
                difficulties={history.data?.difficulties ?? []}
                targetId={effectiveTargetId}
                baseId={baseId}
                onSelect={selectEntry}
                onPin={setPinning}
                onUnpin={(entry) => savePin(entry, null)}
              />
            </SectionCard>

            <Flex
              direction="column"
              gap="md"
              style={{
                flex: 1,
                minWidth: 0,
                // The last comparison stays up while the next one loads, a little dimmed.
                opacity: compare.isPlaceholderData ? 0.55 : 1,
                transition: 'opacity 0.15s',
              }}
              aria-busy={compare.isPlaceholderData}
              w="100%"
            >
              {!hasRange ? (
                <SectionCard title="Changes">
                  <EmptyState
                    icon={IconPointer}
                    title="Only one snapshot so far"
                    description="Once the mapset changes, this shows what changed since the snapshot before."
                  />
                </SectionCard>
              ) : !comparison ? (
                compare.isError ? (
                  <Alert
                    icon={<IconAlertCircle />}
                    color="red"
                    title="Couldn't compare the snapshots"
                  >
                    <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                      {compare.error?.message}
                    </Text>
                  </Alert>
                ) : (
                  <HistorySkeleton />
                )
              ) : (
                <>
                  <SelectedDifficultyRow
                    icons={
                      <>
                        <SnapshotDifficultyChangesIcon
                          hasChanges={
                            selectedDifficulty ? hasChanges(selectedDifficulty) : generalChanged
                          }
                          size={32}
                        />
                        {selectedDifficulty && (
                          <GameModeIcon
                            mode={selectedDifficulty.mode}
                            size={32}
                            starRating={
                              selectedDifficulty.starsAfter ?? selectedDifficulty.starsBefore
                            }
                          />
                        )}
                      </>
                    }
                    name={selectedDifficulty?.name ?? 'General'}
                    badges={
                      !!(selectedDifficulty?.starsAfter ?? selectedDifficulty?.starsBefore) && (
                        <StarRatingBadge
                          rating={
                            (selectedDifficulty!.starsAfter ?? selectedDifficulty!.starsBefore)!
                          }
                        />
                      )
                    }
                  />
                  {selectedDifficulty ? (
                    <DifficultyChanges
                      key={`${selectedDifficulty.key}-${comparison.base.id}-${comparison.target.id}`}
                      difficulty={selectedDifficulty}
                      setKey={history.data!.setKey}
                      baseId={comparison.base.id}
                      targetId={comparison.target.id}
                      general={comparison.general}
                      onViewGeneral={() => setSelectedId(GENERAL_TAB_ID)}
                    />
                  ) : (
                    <GeneralChanges
                      general={comparison.general}
                      difficultyCount={comparison.difficulties.length}
                    />
                  )}
                </>
              )}
            </Flex>
          </Flex>
        </Flex>
      )}

      <PinModal
        key={pinning?.id ?? 'none'}
        opened={pinning !== null}
        initialName={pinning?.pin ?? ''}
        onClose={() => setPinning(null)}
        onSave={(name) => {
          if (pinning) savePin(pinning, name);
          setPinning(null);
        }}
      />
    </>
  );
}

export default Snapshots;
