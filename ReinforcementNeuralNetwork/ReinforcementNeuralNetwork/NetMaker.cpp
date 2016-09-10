#include "stdafx.h"
#include <iostream>

#include "floatfann.h"
#include "fann_cpp.h"

using namespace std;

class NetMaker {
	private:
		unsigned int* layerDetails;
		int size;

	public:
		NetMaker(unsigned int* array, int size, const char *filename) {
			this->layerDetails = new unsigned int[size];
			this->size = size;

			for (int i = 0; i < size; i++) {
				this->layerDetails[i] = array[i];
			}
			
			printf("%i\n", size);
			for (int i = 0; i < size; i++) {
				printf("%i ", (layerDetails[i]));
			}

			struct fann *ann = fann_create_standard_array(size, layerDetails);
			//fann_set_learning_rate(ann,0.01f);
			//fann_set_learning_momentum(ann, 0.9f);
			fann_set_activation_function_hidden(ann, FANN_SIGMOID);
			fann_set_activation_function_output(ann, FANN_SIGMOID);

			fann_save(ann,filename);
			fann_destroy(ann);
		}

		virtual ~NetMaker() {
			delete[] layerDetails;
		}
};