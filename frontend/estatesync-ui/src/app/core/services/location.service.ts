import { Injectable } from '@angular/core';
import { Country, State, City, ICountry, IState, ICity } from 'country-state-city';

@Injectable({ providedIn: 'root' })
export class LocationService {
  getCountries(): ICountry[] {
    return Country.getAllCountries();
  }

  getStates(countryCode: string): IState[] {
    return State.getStatesOfCountry(countryCode);
  }

  getCities(countryCode: string, stateCode: string): ICity[] {
    return City.getCitiesOfState(countryCode, stateCode);
  }
}
