import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExportManagerComponent } from './export-manager.component';

describe('ExportManagerComponent', () => {
  let component: ExportManagerComponent;
  let fixture: ComponentFixture<ExportManagerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExportManagerComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ExportManagerComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
