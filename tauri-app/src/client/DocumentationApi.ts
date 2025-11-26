import { ApiDocumentationCheck, ApiDocumentationCheckDetails, Mode } from '../Types.ts';
import { apiFetch } from './ApiHelper.ts';

const DocumentationApi = {
  getGeneralDocumentation: async function fetchGeneralDocumentation() {
    return apiFetch('/documentation/general').then(
      (res) => res.json() as Promise<ApiDocumentationCheck[]>
    );
  },
  getBeatmapDocumentation: async function fetchGeneralDocumentation(mode: Mode) {
    return apiFetch('/documentation/beatmap?mode=' + mode).then(
      (res) => res.json() as Promise<ApiDocumentationCheck[]>
    );
  },
  getCheckDetails: async function fetchCheckDescription(checkId: string) {
    return apiFetch('/documentation/' + checkId + '/details').then(
      (res) => res.json() as Promise<ApiDocumentationCheckDetails>
    );
  },
};

export default DocumentationApi;
