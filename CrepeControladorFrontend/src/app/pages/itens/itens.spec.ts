import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';

import { Itens } from './itens';

describe('Itens', () => {
  let component: Itens;
  let fixture: ComponentFixture<Itens>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, Itens]
    }).compileComponents();

    fixture = TestBed.createComponent(Itens);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
