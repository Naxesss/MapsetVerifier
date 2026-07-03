import { ApiPluginReport } from '../Types.ts';
import { apiFetch } from './ApiHelper.ts';

const PluginApi = {
  getPlugins: async function fetchPlugins() {
    return apiFetch('/plugins').then((res) => res.json() as Promise<ApiPluginReport>);
  },
};

export default PluginApi;
