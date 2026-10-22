import { notifications } from '@mantine/notifications';
import { IconAlertCircle, IconCheck } from '@tabler/icons-react';

/** In-app error toast; use instead of `alert()`. Say what failed and, when possible, what to try. */
export function notifyError(message: string) {
  notifications.show({
    message,
    color: 'red',
    icon: <IconAlertCircle size={16} />,
    autoClose: 5000,
  });
}

export function notifySuccess(message: string) {
  notifications.show({
    message,
    color: 'green',
    icon: <IconCheck size={16} />,
  });
}

/** Opens a folder or file with the OS default handler, reporting failures as a toast. */
export async function openPathOrNotify(path: string, failureMessage: string) {
  try {
    const err = await window.electronAPI?.shell.openPath(path);
    if (err) throw new Error(err);
  } catch (e) {
    console.error(failureMessage, e);
    notifyError(failureMessage);
  }
}
