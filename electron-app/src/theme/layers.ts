/** Height of the custom title bar; keep in sync with `--mv-window-bar-height` in global.scss. */
export const WINDOW_BAR_HEIGHT = 32;

/** Height of the page navbar below the title bar. */
export const NAV_BAR_HEIGHT = 60;

/**
 * Stacking order of the app's own layers, lowest first. Mantine's own popovers, menus and tooltips
 * sit at 300.
 */
export const Z_INDEX = {
  /** A page's sticky controls (the Overview's difficulty chips), above its tables and timeline. */
  stickyControls: 20,
  /** Modals opened from a page (updater, settings dialogs). */
  modal: 400,
  /** The issue details drawer, which sits below the title bar. */
  drawer: 1900,
  /** The documentation panel, which opens on top of the issue details drawer from its rule links. */
  referencePanel: 1950,
  /** The custom title bar, and modals that open on top of the drawer. */
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
