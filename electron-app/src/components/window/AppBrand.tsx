import { Badge, useMantineTheme } from '@mantine/core';
import { useAppVersion } from './useAppVersion';
import iconUrl from '../../assets/icon.png';

interface AppBrandProps {
  /** Show "Mapset Verifier" next to the logo; the version always shows. */
  showName?: boolean;
}

/**
 * The app's logo, name and version. It stays in the top bar on every page, so a screenshot of the
 * window always says which build it came from. A beta or dev build's tag follows as a badge:
 * "Mapset Verifier 2.3 [beta.2]".
 */
export default function AppBrand({ showName = true }: AppBrandProps) {
  const theme = useMantineTheme();
  const { number, tag, tagKind } = useAppVersion();

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 8,
        whiteSpace: 'nowrap',
        flexShrink: 0,
        userSelect: 'none',
      }}
    >
      <img
        src={iconUrl}
        alt=""
        width={18}
        height={18}
        draggable={false}
        style={{ display: 'block', objectFit: 'contain', flexShrink: 0 }}
      />
      {/* Name and version in the same heading font, the version lighter and faded. */}
      <span
        style={{
          display: 'flex',
          alignItems: 'baseline',
          gap: 4,
          fontFamily: theme.headings.fontFamily,
          fontSize: 14,
          letterSpacing: 0.5,
          color: theme.colors.primary[2],
        }}
      >
        {showName && <span style={{ fontWeight: 600 }}>Mapset Verifier</span>}
        <span style={{ fontWeight: 400, opacity: 0.7 }}>{number}</span>
      </span>
      {tag && (
        // Red for dev, orange for beta, slightly faded; one size up from the theme's xs so it holds
        // its own next to the 14px name.
        <Badge
          color={tagKind === 'dev' ? 'red' : 'orange'}
          radius="sm"
          size="md"
          px={6}
          style={{ flexShrink: 0, opacity: 0.85 }}
        >
          {tag}
        </Badge>
      )}
    </div>
  );
}
