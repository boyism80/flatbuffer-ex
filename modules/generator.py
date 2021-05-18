import jinja2
import os
import copy
import re
import modules.extractor as extractor

def loadTemplateFiles(root):
    loader = jinja2.FileSystemLoader(root)
    env = jinja2.Environment(loader=loader)

    return {os.path.splitext(x)[0]:env.get_template(x) for x in os.listdir(root) if x.endswith('.txt')}

templates = loadTemplateFiles('templates')

def __nameset(name):
    return {
        'base': name,
        'upper': name[0].upper() + name[1:],
        'lower': name[0].lower() + name[1:],
        'upper_snake': re.sub('(?!^)([A-Z]+)', r'_\1',name).upper(),
        'lower_snake': re.sub('(?!^)([A-Z]+)', r'_\1',name).lower()
    }


def go(root, package, dataSet):

    dataSet = copy.deepcopy(dataSet)
    for data in dataSet:
        data['name'] = __nameset(data['name'])
        for param in data['params']:
            param['name'] = __nameset(param['name'])
            param['type'] = extractor.py2go(param['type'])
            if 'array' in param:
                param['element']['name'] = __nameset(extractor.py2go(param['element']['name']))

    return templates['go'].render({
        'root': root.replace('\\', '/'),
        'package': package,
        'dataSet': dataSet
    })

def cs(namespace, dataSet):
    dataSet = copy.deepcopy(dataSet)
    for data in dataSet:
        data['name'] = __nameset(data['name'])
        for param in data['params']:
            param['name'] = __nameset(param['name'])
            param['type'] = extractor.py2cs(param['type'])
            if 'array' in param:
                param['element']['name'] = __nameset(extractor.py2cs(param['element']['name']))

    return templates['cs'].render({
        'namespace': namespace,
        'dataSet': dataSet
    })