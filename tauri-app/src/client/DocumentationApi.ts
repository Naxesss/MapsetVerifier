import {ApiDocumentationCheck, ApiDocumentationCheckDetails, Mode} from "../Types.ts";

const BASE_URL = "http://localhost:5005";

function apiFetch(path: string, options?: RequestInit) {
  return fetch(`${BASE_URL}${path}`, options);
}


export default function DocumentationApi() {
  return {
    getGeneralDocumentation: async function fetchGeneralDocumentation() {
      return apiFetch('/documentation/general')
        .then(res => res.json() as Promise<ApiDocumentationCheck[]>)
    },
    getBeatmapDocumentation: async function fetchGeneralDocumentation(mode: Mode) {
      return apiFetch('/documentation/beatmap?mode=' + mode)
        .then(res => res.json() as Promise<ApiDocumentationCheck[]>)
    },
    getCheckDetails: async function fetchCheckDescription(checkId: string) {
      return apiFetch('/documentation/' + checkId + '/details')
        .then(res => res.json() as Promise<ApiDocumentationCheckDetails>)
    }
  }
}