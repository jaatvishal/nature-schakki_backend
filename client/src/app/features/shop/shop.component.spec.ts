import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ShopService } from '../../core/services/shop.service';
import { ShopComponent } from './shop.component';

describe('ShopComponent search', () => {
  it('reloads all products when search is cleared or becomes empty', async () => {
    const getProducts = vi.fn().mockReturnValue(of({
      pageIndex: 1,
      pageSize: 20,
      count: 1,
      data: [{ id: 1, name: 'Atta', description: '', price: 100, pictureUrl: '', brand: 'NC', type: 'Flour', quantityInStock: 10 }],
    }));

    await TestBed.configureTestingModule({
      imports: [ShopComponent],
      providers: [
        {
          provide: ShopService,
          useValue: { getProducts, getBrands: vi.fn(), getTypes: vi.fn() },
        },
        { provide: MatDialog, useValue: { open: vi.fn() } },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParams: {} } } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ShopComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.shopParams.search = 'atta';
    component.clearSearch();
    expect(component.shopParams.search).toBe('');
    expect(getProducts).toHaveBeenLastCalledWith(component.shopParams);

    const previousCalls = getProducts.mock.calls.length;
    component.shopParams.search = '';
    component.onSearchInput('');
    expect(getProducts).toHaveBeenCalledTimes(previousCalls + 1);
  });
});
