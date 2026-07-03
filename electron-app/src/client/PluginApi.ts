import { ApiPluginReport } from '../Types.ts';
import { apiFetch } from './ApiHelper.ts';

const PluginApi = {
  getPlugins: async function fetchPlugins() {
    return apiFetch('/plugins').then((res) => res.json() as Promise<ApiPluginReport>);
  },

  reloadPlugins: async function reloadPlugins(customChecksEnabled = true) {
    return apiFetch('/plugins/reload', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ customChecksEnabled }),
    }).then((res) => res.json() as Promise<ApiPluginReport>);
  },
};

export default PluginApi;
