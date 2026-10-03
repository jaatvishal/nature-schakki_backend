import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Pagination } from '../../shared/models/pagination';
import { Product } from '../../shared/models/product';
import { ShopParams } from '../../shared/models/shopparams';

@Injectable({
  providedIn: 'root',
})
export class ShopService {
  baseUrl = environment.apiUrl + '/';
  private http= inject(HttpClient);
  types :string[]=[];
  brands :string[]=[];

  getProducts(shopParams: ShopParams) {
    let params= new HttpParams();
     if(shopParams.brands && shopParams.brands.length>0){
      params =params.append('brands',shopParams.brands.join(','));
     }
     if(shopParams.types && shopParams.types.length>0){
      params =params.append('types',shopParams.types.join(','));
     }
     if(shopParams.sort){
      params =params.append('sort',shopParams.sort);
     }
     if(shopParams.search){
      params =params.append('search',shopParams.search);
     }
     params= params.append('pageSize',shopParams.pageSize);
     params= params.append('pageIndex',shopParams.pageNumber);

    return this.http.get<Pagination<Product>>(this.baseUrl + 'product',{params});
  }
  getProduct(id: number) {
    return this.http.get<Product>(this.baseUrl + 'product/' + id);
  }

  getBrands() {
    if(this.brands.length > 0) return ;
    return this.http.get<string[]>(this.baseUrl + 'product/brands').subscribe(response => this.brands = response);
  }
  getTypes() {
    if(this.types.length > 0) return ;
    return this.http.get<string[]>(this.baseUrl + 'product/types').subscribe(response => this.types = response);
  }
}
