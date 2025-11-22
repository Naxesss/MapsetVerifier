import {ApiBeatmapPage} from "../Types.ts";
import {apiFetch, FetchError} from "./ApiHelper.ts";

const BeatmapApi = {
  get: async function fetchGeneralDocumentation(params: URLSearchParams) {
    return apiFetch(`/beatmaps?${params.toString()}`)
        .then(async (response) => {
          const data = await response.json();

          if (response.ok) {
            return data;
          } else {
            throw new FetchError(response)
          }
        }
      ) as Promise<ApiBeatmapPage>;
  }
}

export default BeatmapApi;
