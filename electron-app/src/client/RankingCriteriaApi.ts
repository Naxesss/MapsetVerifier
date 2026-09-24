import { ApiRcOverview, ApiRcPage, ApiRcStatement } from '../Types.ts';
import { apiFetch } from './ApiHelper.ts';

const RankingCriteriaApi = {
  getOverview: async function fetchRankingCriteriaOverview() {
    return apiFetch('/ranking-criteria').then((res) => res.json() as Promise<ApiRcOverview>);
  },
  getPage: async function fetchRankingCriteriaPage(key: string) {
    return apiFetch('/ranking-criteria/pages/' + encodeURIComponent(key)).then(
      (res) => res.json() as Promise<ApiRcPage>
    );
  },
  getStatements: async function fetchRankingCriteriaStatements(ids: string[]) {
    const query = ids.map((id) => 'ids=' + encodeURIComponent(id)).join('&');
    return apiFetch('/ranking-criteria/statements?' + query).then(
      (res) => res.json() as Promise<ApiRcStatement[]>
    );
  },
};

export default RankingCriteriaApi;
