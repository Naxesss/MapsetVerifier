import { Badge, Group } from '@mantine/core';
import { StatField } from '../common/StatField.tsx';

/** The difficulties that use a file, as a badge each: the same wherever a file says who uses it. */
export default function UsedByField({ difficulties }: { difficulties: string[] }) {
  return (
    <StatField
      label="Used by"
      value={
        difficulties.length === 0 ? (
          'No difficulty'
        ) : (
          <Group gap="xs">
            {difficulties.map((difficulty) => (
              <Badge key={difficulty}>{difficulty}</Badge>
            ))}
          </Group>
        )
      }
    />
  );
}
