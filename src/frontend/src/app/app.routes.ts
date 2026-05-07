import { Routes } from '@angular/router';
import { Privacy } from './privacy';
import { Terms } from './terms';
import { Success } from './success';

export const routes: Routes = [
  { path: 'privacy', component: Privacy },
  { path: 'terms', component: Terms },
  { path: 'success', component: Success },
];
