import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GradesListComponent } from './grades-list.component';

describe('GradesListComponent', () => {
  let component: GradesListComponent;
  let fixture: ComponentFixture<GradesListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GradesListComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(GradesListComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
