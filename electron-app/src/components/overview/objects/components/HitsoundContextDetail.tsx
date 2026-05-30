import { Box, Group, Stack, Text, Tooltip } from '@mantine/core';
import type { CrosshairResolvedRow } from '../crosshairUtils.ts';
import {
  formatSampleBankLine,
  getHitsoundTypesFromFlags,
  getSamplesetColor,
  HITSOUND_FLAG_NORMAL,
  hasSliderBodyWhistle,
  type HitsoundTypeDisplay,
} from '../hitsoundUtils.ts';
import { formatEditorTimestamp } from '../timelineUtils.ts';

function HitsoundTypeSwatch({ type }: { type: HitsoundTypeDisplay }) {
  const isSecondary = type.role === 'ring';

  return (
    <Tooltip label={isSecondary ? 'Stacked addition' : 'Primary addition'}>
      <Group gap={4} wrap="nowrap" style={{ cursor: 'help' }}>
        <Box
          style={{
            width: 10,
            height: 10,
            borderRadius: isSecondary ? '50%' : 2,
            background: type.color,
            boxShadow: isSecondary
              ? `0 0 0 1px ${type.color}, 0 0 0 2px rgba(255, 255, 255, 0.25)`
              : undefined,
            flexShrink: 0,
            opacity: isSecondary ? 0.65 : 1,
          }}
        />
        <Text size="xs" c={isSecondary ? 'dimmed' : undefined}>
          {type.label}
        </Text>
      </Group>
    </Tooltip>
  );
}

function SampleBankLine({
  label,
  sampleset,
  customIndex,
  hint,
}: {
  label: string;
  sampleset: string;
  customIndex: number;
  hint?: string;
}) {
  return (
    <Group gap={6} wrap="nowrap" align="flex-start">
      <Box
        style={{
          width: 10,
          height: 10,
          borderRadius: 2,
          background: getSamplesetColor(sampleset),
          border: '1px solid rgba(255, 255, 255, 0.25)',
          flexShrink: 0,
          marginTop: 2,
        }}
      />
      <Stack gap={0}>
        <Text size="xs" c="dimmed">
          {label}: {formatSampleBankLine(sampleset, customIndex)}
        </Text>
        {hint ? (
          <Text size="xs" c="dimmed" fs="italic">
            {hint}
          </Text>
        ) : null}
      </Stack>
    </Group>
  );
}

function MatchKindBadge({ kind }: { kind: CrosshairResolvedRow['matchKind'] }) {
  const label =
    kind === 'edge'
      ? 'Edge lane'
      : kind === 'body-sample' || kind === 'tick-sample'
        ? 'Passive lane'
        : kind === 'slider-body' || kind === 'spinner-body' || kind === 'hold-body'
          ? 'Object body'
          : null;

  if (!label) {
    return null;
  }

  return (
    <Text size="xs" c="dimmed" tt="uppercase" fw={600} style={{ letterSpacing: '0.04em' }}>
      {label}
    </Text>
  );
}

export function HitsoundContextDetail({
  resolved,
  timestampMs,
  compact = false,
}: {
  resolved: CrosshairResolvedRow;
  timestampMs?: number;
  compact?: boolean;
}) {
  const isEdge = resolved.matchKind === 'edge';
  const isSliderBody =
    resolved.matchKind === 'slider-body' ||
    resolved.matchKind === 'body-sample' ||
    resolved.matchKind === 'tick-sample';
  const isLongBody =
    resolved.matchKind === 'spinner-body' || resolved.matchKind === 'hold-body';
  const edgeAdditions = getHitsoundTypesFromFlags(
    resolved.hitSoundFlags || HITSOUND_FLAG_NORMAL
  );
  const bodyAdditions = getHitsoundTypesFromFlags(
    resolved.bodyHitSoundFlags || HITSOUND_FLAG_NORMAL
  );
  const showBodyAdditions =
    isSliderBody && hasSliderBodyWhistle(resolved.bodyHitSoundFlags);
  const playheadOnPassiveSample =
    timestampMs != null &&
    resolved.nearestPassiveSampleTimeMs != null &&
    Math.abs(timestampMs - resolved.nearestPassiveSampleTimeMs) <= 2;

  return (
    <Stack gap={compact ? 3 : 4}>
      {!compact ? (
        <Group gap={8} wrap="wrap" align="center">
          <Text size="sm" fw={500}>
            {resolved.partName}
          </Text>
          <MatchKindBadge kind={resolved.matchKind} />
        </Group>
      ) : (
        <MatchKindBadge kind={resolved.matchKind} />
      )}

      {isEdge ? (
        <Stack gap={4}>
          <Group gap={6} wrap="wrap" align="center">
            <Text size="xs" c="dimmed">
              Edge additions
            </Text>
            {edgeAdditions.map((type) => (
              <HitsoundTypeSwatch key={`${type.label}-${type.role}`} type={type} />
            ))}
          </Group>
          {resolved.sample ? (
            <SampleBankLine
              label="Sample bank"
              sampleset={resolved.sample.sampleset}
              customIndex={resolved.sample.customIndex}
              hint="Edge lane bar colour"
            />
          ) : (
            <Text size="xs" c="dimmed">
              No resolved sample bank at this edge
            </Text>
          )}
        </Stack>
      ) : null}

      {isSliderBody || isLongBody ? (
        <Stack gap={4}>
          {showBodyAdditions ? (
            <Group gap={6} wrap="wrap" align="center">
              <Text size="xs" c="dimmed">
                Body additions
              </Text>
              {bodyAdditions.map((type) => (
                <HitsoundTypeSwatch key={`body-${type.label}-${type.role}`} type={type} />
              ))}
            </Group>
          ) : isSliderBody ? (
            <Text size="xs" c="dimmed">
              Body additions: None (sliderslide only)
            </Text>
          ) : null}

          {resolved.slideSample ? (
            <SampleBankLine
              label="Sliderslide bank"
              sampleset={resolved.slideSample.sampleset}
              customIndex={resolved.slideSample.customIndex}
              hint="Slider line tint and slide dash"
            />
          ) : resolved.matchKind === 'slider-body' ? (
            <Text size="xs" c="dimmed">
              Sliderslide bank: unresolved at playhead
            </Text>
          ) : null}

          {resolved.whistleSample ? (
            <SampleBankLine
              label="Sliderwhistle bank"
              sampleset={resolved.whistleSample.sampleset}
              customIndex={resolved.whistleSample.customIndex}
              hint="Whistle dash in passive lane"
            />
          ) : showBodyAdditions ? (
            <Text size="xs" c="dimmed">
              Sliderwhistle bank: same as sliderslide (Auto addition)
            </Text>
          ) : null}

          {resolved.matchKind === 'tick-sample' && resolved.sample ? (
            <SampleBankLine
              label="Slidertick bank"
              sampleset={resolved.sample.sampleset}
              customIndex={resolved.sample.customIndex}
              hint="Tick dot in passive lane"
            />
          ) : null}

          {resolved.matchKind === 'slider-body' &&
          resolved.nearestPassiveSampleTimeMs != null &&
          !playheadOnPassiveSample ? (
            <Text size="xs" c="dimmed">
              Nearest passive marker: {formatEditorTimestamp(resolved.nearestPassiveSampleTimeMs)}
            </Text>
          ) : null}
        </Stack>
      ) : null}

      {resolved.timingSegment?.sampleset &&
      resolved.timingSegment.sampleset !== 'Auto' ? (
        <SampleBankLine
          label="Section sample bank"
          sampleset={resolved.timingSegment.sampleset}
          customIndex={resolved.timingSegment.customIndex ?? 1}
          hint="Row background tint when Sample bank layer is on"
        />
      ) : null}

      {resolved.timelineObject && !compact ? (
        <Text size="xs" c="dimmed">
          Object: {resolved.timelineObject.objectType}
          {resolved.timelineObject.objectType === 'Slider' ? ' · body hitsound on object' : ''}
        </Text>
      ) : null}
    </Stack>
  );
}
