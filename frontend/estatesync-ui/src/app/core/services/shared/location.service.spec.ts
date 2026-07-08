import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';

// Mock the heavy country-state-city library so tests run fast with no I/O.
vi.mock('country-state-city', () => ({
  Country: { getAllCountries: vi.fn(() => [{ isoCode: 'IN', name: 'India' }]) },
  State:   { getStatesOfCountry: vi.fn(() => [{ isoCode: 'KA', name: 'Karnataka' }]) },
  City:    { getCitiesOfState: vi.fn(() => [{ name: 'Bengaluru' }]) },
}));

import { LocationService } from './location.service';

describe('LocationService', () => {
  let service: LocationService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [LocationService] });
    service = TestBed.inject(LocationService);
  });

  it('returns all countries from the library', () => {
    const countries = service.getCountries();
    expect(countries).toHaveLength(1);
    expect(countries[0].name).toBe('India');
  });

  it('returns states for a given country code', () => {
    const states = service.getStates('IN');
    expect(states).toHaveLength(1);
    expect(states[0].name).toBe('Karnataka');
  });

  it('returns cities for a given country and state code', () => {
    const cities = service.getCities('IN', 'KA');
    expect(cities).toHaveLength(1);
    expect(cities[0].name).toBe('Bengaluru');
  });
});
