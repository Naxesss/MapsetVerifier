import { useNavBarShown } from './navBarShown';
import TopBar from './TopBar';
import { TOP_BAR_HEIGHT, Z_INDEX } from '../../theme/layers';

/**
 * The top bar for the screens before the app is ready (backend start, setup). It is the nav bar
 * without the page links, so when the nav bar takes over nothing jumps.
 */
export default function WindowBar() {
  const navBarShown = useNavBarShown();

  if (navBarShown) return null;

  return (
    <div
      className="mv-top-bar"
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        width: '100%',
        height: TOP_BAR_HEIGHT,
        zIndex: Z_INDEX.topBar,
      }}
    >
      <TopBar />
    </div>
  );
}
