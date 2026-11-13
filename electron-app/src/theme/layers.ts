/**
 * Height of the bar at the top of the window: the window bar during startup and setup, then the nav
 * bar. Keep in sync with `--mv-top-chrome-height` in global.scss.
 */
export const NAV_BAR_HEIGHT = 52;

/**
 * Stacking order of the app's own layers, lowest first. Mantine's own popovers, menus and tooltips
 * sit at 300.
 */
export const Z_INDEX = {
  /** A page's sticky controls (the Overview's difficulty chips), above its tables and timeline. */
  stickyControls: 20,
  /** Modals opened from a page (updater, settings dialogs). */
  modal: 400,
  /** The issue details drawer, which sits below the nav bar. */
  drawer: 1900,
  /** The documentation panel, which opens on top of the issue details drawer from its rule links. */
  referencePanel: 1950,
  /**
   * The bar at the top of the window (the window bar during startup and setup, then the nav bar), so
   * the window controls stay reachable, and modals that open on top of the drawer.
   */
  windowBar: 2000,
  modalAboveDrawer: 2000,
  /**
   * Tooltips, menus, popovers and dropdowns, above every panel and modal they can open from (Mantine's
   * own default of 300 hid them behind the drawers).
   */
  floating: 2050,
  /** Toasts, above everything else. */
  notifications: 2100,
} as const;
