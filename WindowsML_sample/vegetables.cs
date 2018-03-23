using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning.Preview;

// vegetables

namespace vegetables
{
    public sealed class VegetablesModelInput
    {
        public VideoFrame data { get; set; }
    }

    public sealed class VegetablesModelOutput
    {
        public IList<string> classLabel { get; set; }
        public IDictionary<string, float> loss { get; set; }
        public VegetablesModelOutput()
        {
            this.classLabel = new List<string>();
            this.loss = new Dictionary<string, float>();

            //追加
            //ツールから出力されたソースではタグの関連付けを行っていないようなので参照したBlogでは次のようにタグをDictionaryに登録していた。
            //classLabelが該当していそうだけど、それも定義していないので出力データは自力で設定する必要がある？
            this.loss.Add("cabbage", float.NaN);
            this.loss.Add("cucmber", float.NaN);
            this.loss.Add("onion", float.NaN);
            this.loss.Add("poteto", float.NaN);
            this.loss.Add("tomato", float.NaN);
        }
    }

    public sealed class VegetablesModel
    {
        private LearningModelPreview learningModel;
        public static async Task<VegetablesModel> CreateVegetablesModel(StorageFile file)
        {
            LearningModelPreview learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
            VegetablesModel model = new VegetablesModel();
            model.learningModel = learningModel;
            return model;
        }
        public async Task<VegetablesModelOutput> EvaluateAsync(VegetablesModelInput input) {
            VegetablesModelOutput output = new VegetablesModelOutput();
            LearningModelBindingPreview binding = new LearningModelBindingPreview(learningModel);
            binding.Bind("data", input.data);
            binding.Bind("classLabel", output.classLabel);
            binding.Bind("loss", output.loss);
            LearningModelEvaluationResultPreview evalResult = await learningModel.EvaluateAsync(binding, string.Empty);
            return output;
        }
    }
}
