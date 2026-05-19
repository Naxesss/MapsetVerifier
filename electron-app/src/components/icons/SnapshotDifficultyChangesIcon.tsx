import SnapshotHasChangesIcon from './SnapshotHasChangesIcon.tsx';
import SnapshotNoChangesIcon from './SnapshotNoChangesIcon.tsx';

export default function SnapshotDifficultyChangesIcon({
  hasChanges,
  size = 24,
}: {
  hasChanges: boolean;
  size?: number;
}) {
  return hasChanges ? (
    <SnapshotHasChangesIcon size={size} />
  ) : (
    <SnapshotNoChangesIcon size={size} />
  );
}
